using Proximity.Core.Models;

namespace Proximity.Core.Interfaces;

/// <summary>
/// Interface for managing session connections (TCP control channel)
/// </summary>
public interface ISessionManager : IAsyncDisposable
{
    /// <summary>
    /// Create and host a new session
    /// </summary>
    Task<VoiceSession> CreateSessionAsync(string sessionName, string hostUsername, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Join an existing session
    /// </summary>
    Task<bool> JoinSessionAsync(VoiceSession session, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leave the current session
    /// </summary>
    Task LeaveSessionAsync();

    /// <summary>
    /// Kick a participant (host only)
    /// </summary>
    Task KickParticipantAsync(Guid participantId, string reason = "");

    /// <summary>
    /// Send a chat message to the session
    /// </summary>
    Task SendChatMessageAsync(string content);

    /// <summary>
    /// Send mute state change notification
    /// </summary>
    Task SendMuteStateAsync(bool isMuted);

    /// <summary>
    /// Whether we're currently connected to a session
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Whether we're the host of the current session
    /// </summary>
    bool IsHost { get; }

    /// <summary>
    /// Our participant ID in the current session
    /// </summary>
    Guid LocalParticipantId { get; }

    /// <summary>
    /// The current session we're in
    /// </summary>
    VoiceSession? CurrentSession { get; }

    /// <summary>
    /// Event raised when a participant joins
    /// </summary>
    event EventHandler<Participant> ParticipantJoined;

    /// <summary>
    /// Event raised when a participant leaves
    /// </summary>
    event EventHandler<Guid> ParticipantLeft;

    /// <summary>
    /// Event raised when the participant list is updated
    /// </summary>
    event EventHandler<List<Participant>> ParticipantListUpdated;

    /// <summary>
    /// Event raised when a chat message is received
    /// </summary>
    event EventHandler<ChatMessage> ChatMessageReceived;

    /// <summary>
    /// Event raised when we are kicked from the session
    /// </summary>
    event EventHandler<string> Kicked;

    /// <summary>
    /// Event raised when disconnected from the session
    /// </summary>
    event EventHandler<string> Disconnected;
}
