namespace Proximity.Core.Models;

/// <summary>
/// Represents a participant in a voice session
/// </summary>
public class Participant
{
    /// <summary>
    /// Unique identifier for this participant
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name chosen by the user
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// IP endpoint of this participant
    /// </summary>
    public string EndPoint { get; set; } = string.Empty;

    /// <summary>
    /// Whether this participant is the session host
    /// </summary>
    public bool IsHost { get; set; }

    /// <summary>
    /// Whether this participant has muted themselves
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// Local volume level for this participant (0.0 to 1.0)
    /// </summary>
    public float Volume { get; set; } = 1.0f;

    /// <summary>
    /// Whether this participant is locally muted by the current user
    /// </summary>
    public bool IsLocallyMuted { get; set; }

    /// <summary>
    /// Whether this participant is currently speaking
    /// </summary>
    public bool IsSpeaking { get; set; }

    /// <summary>
    /// When this participant joined the session
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
