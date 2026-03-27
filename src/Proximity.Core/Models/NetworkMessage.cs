using System.Text.Json;
using System.Text.Json.Serialization;

namespace Proximity.Core.Models;

/// <summary>
/// Types of control messages exchanged between participants
/// </summary>
public enum MessageType
{
    /// <summary>Session discovery announcement (UDP broadcast)</summary>
    SessionAnnounce,
    /// <summary>Request to join a session</summary>
    JoinRequest,
    /// <summary>Response to a join request</summary>
    JoinResponse,
    /// <summary>Notification that a participant has left</summary>
    Leave,
    /// <summary>Updated participant list</summary>
    ParticipantList,
    /// <summary>Chat message</summary>
    Chat,
    /// <summary>Kick a participant (host only)</summary>
    Kick,
    /// <summary>Participant mute state changed</summary>
    MuteStateChanged,
    /// <summary>Heartbeat to keep connection alive</summary>
    Heartbeat
}

/// <summary>
/// A control message sent over TCP between session participants
/// </summary>
public class NetworkMessage
{
    /// <summary>
    /// Type of this message
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// ID of the sender
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// JSON payload for this message
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Serialize this message to a JSON string
    /// </summary>
    public string Serialize()
    {
        return JsonSerializer.Serialize(this, JsonContext.Default.NetworkMessage);
    }

    /// <summary>
    /// Deserialize a JSON string to a NetworkMessage
    /// </summary>
    public static NetworkMessage? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize(json, JsonContext.Default.NetworkMessage);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Create a message with a typed payload
    /// </summary>
    public static NetworkMessage Create<T>(MessageType type, Guid senderId, T payload)
    {
        return new NetworkMessage
        {
            Type = type,
            SenderId = senderId,
            Payload = JsonSerializer.Serialize(payload)
        };
    }

    /// <summary>
    /// Deserialize the payload to a specific type
    /// </summary>
    public T? GetPayload<T>()
    {
        if (string.IsNullOrEmpty(Payload)) return default;
        return JsonSerializer.Deserialize<T>(Payload);
    }
}

/// <summary>
/// Payload for join request messages
/// </summary>
public class JoinRequestPayload
{
    public Guid ParticipantId { get; set; }
    public string Username { get; set; } = string.Empty;
}

/// <summary>
/// Payload for join response messages
/// </summary>
public class JoinResponsePayload
{
    public bool Accepted { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public int AudioPort { get; set; }
    public List<ParticipantInfo> Participants { get; set; } = new();
}

/// <summary>
/// Lightweight participant info for network transfer
/// </summary>
public class ParticipantInfo
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public bool IsMuted { get; set; }
}

/// <summary>
/// Payload for participant list updates
/// </summary>
public class ParticipantListPayload
{
    public List<ParticipantInfo> Participants { get; set; } = new();
}

/// <summary>
/// Payload for chat messages
/// </summary>
public class ChatPayload
{
    public string Content { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Payload for kick messages
/// </summary>
public class KickPayload
{
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Payload for mute state changes
/// </summary>
public class MuteStatePayload
{
    public bool IsMuted { get; set; }
}

/// <summary>
/// JSON serialization context for AOT/trim compatibility
/// </summary>
[JsonSerializable(typeof(NetworkMessage))]
internal partial class JsonContext : JsonSerializerContext
{
}
