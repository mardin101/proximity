using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;

namespace Proximity.Network.Transport;

/// <summary>
/// UDP-based audio transport for low-latency audio packet delivery.
/// Audio packets include a sender ID header so receivers can identify the source.
/// </summary>
public class UdpAudioTransport : IAudioTransport
{
    private readonly ILogger<UdpAudioTransport> _logger;

    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private int _localPort;

    private readonly ConcurrentDictionary<Guid, IPEndPoint> _targets = new();

    // Packet format: [16 bytes sender GUID][audio data...]
    private const int HeaderSize = 16;

    public event EventHandler<AudioPacketEventArgs>? AudioReceived;

    public UdpAudioTransport(ILogger<UdpAudioTransport> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        _localPort = port;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _udpClient = new UdpClient();
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        // Set socket buffer sizes for low latency
        _udpClient.Client.ReceiveBufferSize = 65536;
        _udpClient.Client.SendBufferSize = 65536;

        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);

        _logger.LogInformation("Audio transport started on port {Port}", port);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping audio transport");

        if (_cts != null)
        {
            await _cts.CancelAsync();
        }

        if (_receiveTask != null)
        {
            try { await _receiveTask; } catch (OperationCanceledException) { }
        }

        _udpClient?.Dispose();
        _udpClient = null;
        _targets.Clear();
        _cts?.Dispose();
        _cts = null;
    }

    public async Task SendAudioAsync(Guid senderId, byte[] audioData, int count, string remoteAddress, int remotePort)
    {
        if (_udpClient == null) return;

        try
        {
            var packet = new byte[HeaderSize + count];
            senderId.ToByteArray().CopyTo(packet, 0);
            Buffer.BlockCopy(audioData, 0, packet, HeaderSize, count);

            var endpoint = new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort);
            await _udpClient.SendAsync(packet, packet.Length, endpoint);
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error sending audio packet");
        }
    }

    public void AddTarget(Guid participantId, string address, int port)
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(address), port);
        _targets[participantId] = endpoint;
        _logger.LogDebug("Added audio target {ParticipantId} at {EndPoint}", participantId, endpoint);
    }

    public void RemoveTarget(Guid participantId)
    {
        _targets.TryRemove(participantId, out _);
        _logger.LogDebug("Removed audio target {ParticipantId}", participantId);
    }

    public async Task BroadcastAudioAsync(Guid senderId, byte[] audioData, int count)
    {
        if (_udpClient == null || _targets.IsEmpty) return;

        var packet = new byte[HeaderSize + count];
        senderId.ToByteArray().CopyTo(packet, 0);
        Buffer.BlockCopy(audioData, 0, packet, HeaderSize, count);

        var tasks = _targets.Values.Select(async endpoint =>
        {
            try
            {
                await _udpClient.SendAsync(packet, packet.Length, endpoint);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error sending audio to {EndPoint}", endpoint);
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient!.ReceiveAsync(cancellationToken);
                var data = result.Buffer;

                if (data.Length <= HeaderSize) continue;

                var senderId = new Guid(data.AsSpan(0, HeaderSize));
                var audioLength = data.Length - HeaderSize;
                var audioData = new byte[audioLength];
                Buffer.BlockCopy(data, HeaderSize, audioData, 0, audioLength);

                AudioReceived?.Invoke(this, new AudioPacketEventArgs(senderId, audioData, audioLength));
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
                _logger.LogDebug(ex, "Error receiving audio packet");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }
}
