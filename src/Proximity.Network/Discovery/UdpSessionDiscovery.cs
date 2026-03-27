using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;
using Proximity.Core.Models;

namespace Proximity.Network.Discovery;

/// <summary>
/// UDP broadcast-based session discovery for LAN environments.
/// Hosts periodically broadcast their session info; clients listen for announcements.
/// </summary>
public class UdpSessionDiscovery : ISessionDiscovery
{
    private readonly ILogger<UdpSessionDiscovery> _logger;
    private readonly int _discoveryPort;
    private readonly TimeSpan _broadcastInterval = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromSeconds(8);

    private UdpClient? _broadcastClient;
    private UdpClient? _listenerClient;
    private CancellationTokenSource? _broadcastCts;
    private CancellationTokenSource? _discoveryCts;
    private Task? _broadcastTask;
    private Task? _discoveryTask;
    private Task? _cleanupTask;

    private readonly Dictionary<Guid, VoiceSession> _discoveredSessions = new();
    private readonly object _sessionsLock = new();

    public event EventHandler<VoiceSession>? SessionDiscovered;
    public event EventHandler<Guid>? SessionLost;

    public UdpSessionDiscovery(ILogger<UdpSessionDiscovery> logger, int discoveryPort = 7778)
    {
        _logger = logger;
        _discoveryPort = discoveryPort;
    }

    public Task StartBroadcastingAsync(VoiceSession session, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting session broadcast for '{SessionName}' on port {Port}", session.SessionName, _discoveryPort);

        _broadcastCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _broadcastClient = new UdpClient();
        _broadcastClient.EnableBroadcast = true;

        _broadcastTask = Task.Run(async () =>
        {
            var endpoint = new IPEndPoint(IPAddress.Broadcast, _discoveryPort);
            while (!_broadcastCts.Token.IsCancellationRequested)
            {
                try
                {
                    var json = JsonSerializer.Serialize(session);
                    var data = Encoding.UTF8.GetBytes(json);
                    await _broadcastClient.SendAsync(data, data.Length, endpoint);
                    _logger.LogDebug("Broadcast session announcement: {SessionName}", session.SessionName);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error broadcasting session");
                }

                try
                {
                    await Task.Delay(_broadcastInterval, _broadcastCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, _broadcastCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopBroadcastingAsync()
    {
        _logger.LogInformation("Stopping session broadcast");

        if (_broadcastCts != null)
        {
            await _broadcastCts.CancelAsync();
        }

        if (_broadcastTask != null)
        {
            try { await _broadcastTask; } catch (OperationCanceledException) { }
        }

        _broadcastClient?.Dispose();
        _broadcastClient = null;
        _broadcastCts?.Dispose();
        _broadcastCts = null;
    }

    public Task StartDiscoveryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting session discovery on port {Port}", _discoveryPort);

        _discoveryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenerClient = new UdpClient();
        _listenerClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listenerClient.Client.Bind(new IPEndPoint(IPAddress.Any, _discoveryPort));

        _discoveryTask = Task.Run(async () =>
        {
            while (!_discoveryCts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _listenerClient.ReceiveAsync(_discoveryCts.Token);
                    var json = Encoding.UTF8.GetString(result.Buffer);
                    var session = JsonSerializer.Deserialize<VoiceSession>(json);

                    if (session != null)
                    {
                        session.HostAddress = result.RemoteEndPoint.Address.ToString();
                        session.LastSeen = DateTime.UtcNow;
                        ProcessDiscoveredSession(session);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error receiving discovery broadcast");
                }
            }
        }, _discoveryCts.Token);

        // Start cleanup task to remove stale sessions
        _cleanupTask = Task.Run(async () =>
        {
            while (!_discoveryCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), _discoveryCts.Token);
                    CleanupStaleSessions();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, _discoveryCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopDiscoveryAsync()
    {
        _logger.LogInformation("Stopping session discovery");

        if (_discoveryCts != null)
        {
            await _discoveryCts.CancelAsync();
        }

        if (_discoveryTask != null)
        {
            try { await _discoveryTask; } catch (OperationCanceledException) { }
        }

        if (_cleanupTask != null)
        {
            try { await _cleanupTask; } catch (OperationCanceledException) { }
        }

        _listenerClient?.Dispose();
        _listenerClient = null;
        _discoveryCts?.Dispose();
        _discoveryCts = null;
    }

    private void ProcessDiscoveredSession(VoiceSession session)
    {
        bool isNew;
        lock (_sessionsLock)
        {
            isNew = !_discoveredSessions.ContainsKey(session.SessionId);
            _discoveredSessions[session.SessionId] = session;
        }

        if (isNew)
        {
            _logger.LogInformation("Discovered new session: '{SessionName}' hosted by {HostName} at {HostAddress}",
                session.SessionName, session.HostName, session.HostAddress);
        }

        SessionDiscovered?.Invoke(this, session);
    }

    private void CleanupStaleSessions()
    {
        List<Guid> staleIds;
        lock (_sessionsLock)
        {
            staleIds = _discoveredSessions
                .Where(kvp => DateTime.UtcNow - kvp.Value.LastSeen > _sessionTimeout)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var id in staleIds)
            {
                _discoveredSessions.Remove(id);
            }
        }

        foreach (var id in staleIds)
        {
            _logger.LogInformation("Session {SessionId} timed out and was removed", id);
            SessionLost?.Invoke(this, id);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopBroadcastingAsync();
        await StopDiscoveryAsync();
        GC.SuppressFinalize(this);
    }
}
