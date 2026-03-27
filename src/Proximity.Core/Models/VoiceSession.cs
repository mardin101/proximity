namespace Proximity.Core.Models;

/// <summary>
/// Represents a voice chat session on the local network
/// </summary>
public class VoiceSession
{
    /// <summary>
    /// Unique identifier for this session
    /// </summary>
    public Guid SessionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name for the session
    /// </summary>
    public string SessionName { get; set; } = string.Empty;

    /// <summary>
    /// Username of the session host
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the host
    /// </summary>
    public string HostAddress { get; set; } = string.Empty;

    /// <summary>
    /// TCP port for session control messages
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Current number of participants
    /// </summary>
    public int ParticipantCount { get; set; }

    /// <summary>
    /// Maximum number of participants allowed
    /// </summary>
    public int MaxParticipants { get; set; } = 10;

    /// <summary>
    /// When this session was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this session was last seen (for discovery timeout)
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}
