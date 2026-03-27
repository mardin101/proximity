namespace Proximity.Core.Models;

/// <summary>
/// Represents an ephemeral chat message within a session
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Unique message identifier
    /// </summary>
    public Guid MessageId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the sender
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// Username of the sender
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Message content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the message was sent
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
