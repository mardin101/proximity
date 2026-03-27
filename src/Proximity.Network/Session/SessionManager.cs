using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;
using Proximity.Core.Models;

namespace Proximity.Network.Session;

/// <summary>
/// Manages voice chat sessions using TCP for control messages.
/// Handles hosting (accepting connections) and joining (connecting to host).
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly ILogger<SessionManager> _logger;

    private TcpListener? _listener;
    private TcpClient? _clientConnection;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private Task? _receiveTask;

    private readonly ConcurrentDictionary<Guid, ConnectedClient> _connectedClients = new();
    private readonly ConcurrentDictionary<Guid, Participant> _participants = new();

    public bool IsConnected { get; private set; }
    public bool IsHost { get; private set; }
    public Guid LocalParticipantId { get; private set; }
    public VoiceSession? CurrentSession { get; private set; }

    public event EventHandler<Participant>? ParticipantJoined;
    public event EventHandler<Guid>? ParticipantLeft;
    public event EventHandler<List<Participant>>? ParticipantListUpdated;
    public event EventHandler<ChatMessage>? ChatMessageReceived;
    public event EventHandler<string>? Kicked;
    public event EventHandler<string>? Disconnected;

    public SessionManager(ILogger<SessionManager> logger)
    {
        _logger = logger;
    }

    public async Task<VoiceSession> CreateSessionAsync(string sessionName, string hostUsername, int port, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating session '{SessionName}' on port {Port}", sessionName, port);

        LocalParticipantId = Guid.NewGuid();
        IsHost = true;

        var session = new VoiceSession
        {
            SessionName = sessionName,
            HostName = hostUsername,
            Port = port,
            ParticipantCount = 1,
            HostAddress = GetLocalIPAddress()
        };

        CurrentSession = session;

        // Add ourselves as a participant
        var hostParticipant = new Participant
        {
            Id = LocalParticipantId,
            Username = hostUsername,
            IsHost = true,
            EndPoint = session.HostAddress
        };
        _participants[LocalParticipantId] = hostParticipant;

        // Start TCP listener
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        IsConnected = true;

        _acceptTask = Task.Run(() => AcceptClientsAsync(_cts.Token), _cts.Token);

        _logger.LogInformation("Session created. Listening on port {Port}", port);
        return session;
    }

    public async Task<bool> JoinSessionAsync(VoiceSession session, string username, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Joining session '{SessionName}' at {HostAddress}:{Port}", session.SessionName, session.HostAddress, session.Port);

        LocalParticipantId = Guid.NewGuid();
        IsHost = false;
        CurrentSession = session;

        try
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _clientConnection = new TcpClient();

            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(TimeSpan.FromSeconds(5));

            await _clientConnection.ConnectAsync(session.HostAddress, session.Port, connectCts.Token);

            // Send join request
            var joinRequest = NetworkMessage.Create(MessageType.JoinRequest, LocalParticipantId,
                new JoinRequestPayload { ParticipantId = LocalParticipantId, Username = username });
            await SendMessageAsync(_clientConnection, joinRequest);

            // Wait for response
            var response = await ReceiveMessageAsync(_clientConnection, connectCts.Token);
            if (response?.Type != MessageType.JoinResponse)
            {
                _logger.LogWarning("Unexpected response type: {Type}", response?.Type);
                await CleanupAsync();
                return false;
            }

            var joinResponse = response.GetPayload<JoinResponsePayload>();
            if (joinResponse == null || !joinResponse.Accepted)
            {
                _logger.LogWarning("Join request denied: {Reason}", joinResponse?.Reason ?? "Unknown");
                await CleanupAsync();
                return false;
            }

            IsConnected = true;

            // Update participants from the response
            foreach (var info in joinResponse.Participants)
            {
                _participants[info.Id] = new Participant
                {
                    Id = info.Id,
                    Username = info.Username,
                    IsHost = info.IsHost,
                    IsMuted = info.IsMuted
                };
            }

            // Add ourselves
            _participants[LocalParticipantId] = new Participant
            {
                Id = LocalParticipantId,
                Username = username,
                IsHost = false
            };

            ParticipantListUpdated?.Invoke(this, _participants.Values.ToList());

            // Start receiving messages
            _receiveTask = Task.Run(() => ReceiveFromHostAsync(_cts.Token), _cts.Token);

            _logger.LogInformation("Successfully joined session '{SessionName}'", session.SessionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join session");
            await CleanupAsync();
            return false;
        }
    }

    public async Task LeaveSessionAsync()
    {
        _logger.LogInformation("Leaving session");

        if (IsConnected && !IsHost && _clientConnection != null)
        {
            try
            {
                var leaveMsg = new NetworkMessage { Type = MessageType.Leave, SenderId = LocalParticipantId };
                await SendMessageAsync(_clientConnection, leaveMsg);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error sending leave message");
            }
        }

        await CleanupAsync();
    }

    public async Task KickParticipantAsync(Guid participantId, string reason = "")
    {
        if (!IsHost)
        {
            _logger.LogWarning("Only the host can kick participants");
            return;
        }

        if (_connectedClients.TryGetValue(participantId, out var client))
        {
            var kickMsg = NetworkMessage.Create(MessageType.Kick, LocalParticipantId,
                new KickPayload { TargetId = participantId, Reason = reason });
            try
            {
                await SendMessageAsync(client.TcpClient, kickMsg);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error sending kick message");
            }

            await RemoveClientAsync(participantId, "Kicked: " + reason);
        }
    }

    public async Task SendChatMessageAsync(string content)
    {
        var chatPayload = new ChatPayload
        {
            Content = content,
            SenderName = _participants.TryGetValue(LocalParticipantId, out var self) ? self.Username : "Unknown",
            Timestamp = DateTime.UtcNow
        };

        var msg = NetworkMessage.Create(MessageType.Chat, LocalParticipantId, chatPayload);

        if (IsHost)
        {
            // Broadcast to all clients
            await BroadcastToClientsAsync(msg);
            // Also raise locally
            ChatMessageReceived?.Invoke(this, new ChatMessage
            {
                SenderId = LocalParticipantId,
                SenderName = chatPayload.SenderName,
                Content = content,
                Timestamp = chatPayload.Timestamp
            });
        }
        else if (_clientConnection != null)
        {
            await SendMessageAsync(_clientConnection, msg);
        }
    }

    public async Task SendMuteStateAsync(bool isMuted)
    {
        if (_participants.TryGetValue(LocalParticipantId, out var self))
        {
            self.IsMuted = isMuted;
        }

        var msg = NetworkMessage.Create(MessageType.MuteStateChanged, LocalParticipantId,
            new MuteStatePayload { IsMuted = isMuted });

        if (IsHost)
        {
            await BroadcastToClientsAsync(msg);
            await BroadcastParticipantListAsync();
        }
        else if (_clientConnection != null)
        {
            await SendMessageAsync(_clientConnection, msg);
        }
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Accepting client connections...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener!.AcceptTcpClientAsync(cancellationToken);
                _logger.LogInformation("New connection from {RemoteEndPoint}", tcpClient.Client.RemoteEndPoint);
                _ = Task.Run(() => HandleClientAsync(tcpClient, cancellationToken), cancellationToken);
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
                _logger.LogError(ex, "Error accepting client");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        Guid clientId = Guid.Empty;

        try
        {
            // Wait for join request
            var msg = await ReceiveMessageAsync(tcpClient, cancellationToken);
            if (msg?.Type != MessageType.JoinRequest)
            {
                tcpClient.Dispose();
                return;
            }

            var joinRequest = msg.GetPayload<JoinRequestPayload>();
            if (joinRequest == null)
            {
                tcpClient.Dispose();
                return;
            }

            clientId = joinRequest.ParticipantId;

            // Check capacity
            if (_participants.Count >= (CurrentSession?.MaxParticipants ?? 10))
            {
                var denyMsg = NetworkMessage.Create(MessageType.JoinResponse, LocalParticipantId,
                    new JoinResponsePayload { Accepted = false, Reason = "Session is full" });
                await SendMessageAsync(tcpClient, denyMsg);
                tcpClient.Dispose();
                return;
            }

            // Accept the client
            var participant = new Participant
            {
                Id = clientId,
                Username = joinRequest.Username,
                IsHost = false,
                EndPoint = ((IPEndPoint?)tcpClient.Client.RemoteEndPoint)?.Address.ToString() ?? ""
            };

            _participants[clientId] = participant;
            _connectedClients[clientId] = new ConnectedClient(tcpClient, clientId);

            // Send join response with current participant list
            var response = NetworkMessage.Create(MessageType.JoinResponse, LocalParticipantId,
                new JoinResponsePayload
                {
                    Accepted = true,
                    SessionId = CurrentSession!.SessionId,
                    AudioPort = CurrentSession.Port + 1,
                    Participants = _participants.Values.Select(p => new ParticipantInfo
                    {
                        Id = p.Id,
                        Username = p.Username,
                        IsHost = p.IsHost,
                        IsMuted = p.IsMuted
                    }).ToList()
                });
            await SendMessageAsync(tcpClient, response);

            // Update session participant count
            CurrentSession.ParticipantCount = _participants.Count;

            // Notify others
            ParticipantJoined?.Invoke(this, participant);
            await BroadcastParticipantListAsync();

            _logger.LogInformation("Participant '{Username}' ({Id}) joined the session", participant.Username, clientId);

            // Listen for messages from this client
            while (!cancellationToken.IsCancellationRequested && tcpClient.Connected)
            {
                var clientMsg = await ReceiveMessageAsync(tcpClient, cancellationToken);
                if (clientMsg == null) break;

                await HandleClientMessageAsync(clientId, clientMsg);
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client {ClientId}", clientId);
        }
        finally
        {
            if (clientId != Guid.Empty)
            {
                await RemoveClientAsync(clientId, "Disconnected");
            }
            else
            {
                tcpClient.Dispose();
            }
        }
    }

    private async Task HandleClientMessageAsync(Guid clientId, NetworkMessage msg)
    {
        switch (msg.Type)
        {
            case MessageType.Leave:
                await RemoveClientAsync(clientId, "Left the session");
                break;

            case MessageType.Chat:
                var chatPayload = msg.GetPayload<ChatPayload>();
                if (chatPayload != null)
                {
                    var chatMessage = new ChatMessage
                    {
                        SenderId = clientId,
                        SenderName = chatPayload.SenderName,
                        Content = chatPayload.Content,
                        Timestamp = chatPayload.Timestamp
                    };
                    ChatMessageReceived?.Invoke(this, chatMessage);
                    // Relay to other clients
                    await BroadcastToClientsAsync(msg, excludeId: clientId);
                }
                break;

            case MessageType.MuteStateChanged:
                var mutePayload = msg.GetPayload<MuteStatePayload>();
                if (mutePayload != null && _participants.TryGetValue(clientId, out var participant))
                {
                    participant.IsMuted = mutePayload.IsMuted;
                    await BroadcastParticipantListAsync();
                }
                break;

            case MessageType.Heartbeat:
                break;
        }
    }

    private async Task RemoveClientAsync(Guid clientId, string reason)
    {
        _participants.TryRemove(clientId, out _);

        if (_connectedClients.TryRemove(clientId, out var client))
        {
            try { client.TcpClient.Dispose(); } catch { }
        }

        if (CurrentSession != null)
        {
            CurrentSession.ParticipantCount = _participants.Count;
        }

        _logger.LogInformation("Participant {Id} removed: {Reason}", clientId, reason);
        ParticipantLeft?.Invoke(this, clientId);
        await BroadcastParticipantListAsync();
    }

    private async Task ReceiveFromHostAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _clientConnection?.Connected == true)
            {
                var msg = await ReceiveMessageAsync(_clientConnection, cancellationToken);
                if (msg == null) break;

                switch (msg.Type)
                {
                    case MessageType.ParticipantList:
                        var listPayload = msg.GetPayload<ParticipantListPayload>();
                        if (listPayload != null)
                        {
                            _participants.Clear();
                            foreach (var info in listPayload.Participants)
                            {
                                _participants[info.Id] = new Participant
                                {
                                    Id = info.Id,
                                    Username = info.Username,
                                    IsHost = info.IsHost,
                                    IsMuted = info.IsMuted
                                };
                            }
                            ParticipantListUpdated?.Invoke(this, _participants.Values.ToList());
                        }
                        break;

                    case MessageType.Chat:
                        var chatPayload = msg.GetPayload<ChatPayload>();
                        if (chatPayload != null)
                        {
                            ChatMessageReceived?.Invoke(this, new ChatMessage
                            {
                                SenderId = msg.SenderId,
                                SenderName = chatPayload.SenderName,
                                Content = chatPayload.Content,
                                Timestamp = chatPayload.Timestamp
                            });
                        }
                        break;

                    case MessageType.Kick:
                        var kickPayload = msg.GetPayload<KickPayload>();
                        if (kickPayload?.TargetId == LocalParticipantId)
                        {
                            _logger.LogWarning("Kicked from session: {Reason}", kickPayload.Reason);
                            Kicked?.Invoke(this, kickPayload.Reason);
                            await CleanupAsync();
                            return;
                        }
                        break;

                    case MessageType.Heartbeat:
                        break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving from host");
        }

        if (IsConnected)
        {
            IsConnected = false;
            Disconnected?.Invoke(this, "Connection to host lost");
        }
    }

    private async Task BroadcastToClientsAsync(NetworkMessage msg, Guid? excludeId = null)
    {
        var tasks = _connectedClients
            .Where(kvp => kvp.Key != excludeId)
            .Select(async kvp =>
            {
                try
                {
                    await SendMessageAsync(kvp.Value.TcpClient, msg);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error sending to client {ClientId}", kvp.Key);
                }
            });

        await Task.WhenAll(tasks);
    }

    private async Task BroadcastParticipantListAsync()
    {
        var participantList = new ParticipantListPayload
        {
            Participants = _participants.Values.Select(p => new ParticipantInfo
            {
                Id = p.Id,
                Username = p.Username,
                IsHost = p.IsHost,
                IsMuted = p.IsMuted
            }).ToList()
        };

        var msg = NetworkMessage.Create(MessageType.ParticipantList, LocalParticipantId, participantList);
        await BroadcastToClientsAsync(msg);

        // Also notify local listeners
        ParticipantListUpdated?.Invoke(this, _participants.Values.ToList());
    }

    private static async Task SendMessageAsync(TcpClient client, NetworkMessage msg)
    {
        var json = msg.Serialize();
        var data = Encoding.UTF8.GetBytes(json);
        var lengthPrefix = BitConverter.GetBytes(data.Length);

        var stream = client.GetStream();
        await stream.WriteAsync(lengthPrefix);
        await stream.WriteAsync(data);
        await stream.FlushAsync();
    }

    private static async Task<NetworkMessage?> ReceiveMessageAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();

        // Read length prefix (4 bytes)
        var lengthBuffer = new byte[4];
        var bytesRead = 0;
        while (bytesRead < 4)
        {
            var read = await stream.ReadAsync(lengthBuffer.AsMemory(bytesRead, 4 - bytesRead), cancellationToken);
            if (read == 0) return null; // Connection closed
            bytesRead += read;
        }

        var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
        if (messageLength <= 0 || messageLength > 1024 * 1024) return null; // Sanity check: max 1MB

        // Read message data
        var messageBuffer = new byte[messageLength];
        bytesRead = 0;
        while (bytesRead < messageLength)
        {
            var read = await stream.ReadAsync(messageBuffer.AsMemory(bytesRead, messageLength - bytesRead), cancellationToken);
            if (read == 0) return null; // Connection closed
            bytesRead += read;
        }

        var json = Encoding.UTF8.GetString(messageBuffer);
        return NetworkMessage.Deserialize(json);
    }

    private async Task CleanupAsync()
    {
        IsConnected = false;

        if (_cts != null)
        {
            await _cts.CancelAsync();
        }

        // Dispose all client connections
        foreach (var client in _connectedClients.Values)
        {
            try { client.TcpClient.Dispose(); } catch { }
        }
        _connectedClients.Clear();

        _clientConnection?.Dispose();
        _clientConnection = null;

        try { _listener?.Stop(); } catch { }
        _listener = null;

        if (_acceptTask != null)
        {
            try { await _acceptTask; } catch (OperationCanceledException) { }
        }
        if (_receiveTask != null)
        {
            try { await _receiveTask; } catch (OperationCanceledException) { }
        }

        _participants.Clear();
        CurrentSession = null;
        IsHost = false;
        _cts?.Dispose();
        _cts = null;
    }

    private static string GetLocalIPAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 80);
            return ((IPEndPoint)socket.LocalEndPoint!).Address.ToString();
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupAsync();
        GC.SuppressFinalize(this);
    }

    private sealed record ConnectedClient(TcpClient TcpClient, Guid ParticipantId);
}
