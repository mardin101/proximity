using Proximity.Core.Models;

namespace Proximity.Tests;

public class NetworkMessageTests
{
    [Fact]
    public void Serialize_And_Deserialize_Roundtrip()
    {
        var senderId = Guid.NewGuid();
        var msg = new NetworkMessage
        {
            Type = MessageType.Chat,
            SenderId = senderId,
            Payload = "{\"Content\":\"Hello\"}"
        };

        var json = msg.Serialize();
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        var deserialized = NetworkMessage.Deserialize(json);
        Assert.NotNull(deserialized);
        Assert.Equal(MessageType.Chat, deserialized.Type);
        Assert.Equal(senderId, deserialized.SenderId);
        Assert.Equal("{\"Content\":\"Hello\"}", deserialized.Payload);
    }

    [Fact]
    public void Create_WithTypedPayload_SerializesCorrectly()
    {
        var senderId = Guid.NewGuid();
        var payload = new ChatPayload
        {
            Content = "Test message",
            SenderName = "TestUser",
            Timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var msg = NetworkMessage.Create(MessageType.Chat, senderId, payload);

        Assert.Equal(MessageType.Chat, msg.Type);
        Assert.Equal(senderId, msg.SenderId);
        Assert.NotEmpty(msg.Payload);

        var deserialized = msg.GetPayload<ChatPayload>();
        Assert.NotNull(deserialized);
        Assert.Equal("Test message", deserialized.Content);
        Assert.Equal("TestUser", deserialized.SenderName);
    }

    [Fact]
    public void Create_JoinRequest_SerializesCorrectly()
    {
        var participantId = Guid.NewGuid();
        var msg = NetworkMessage.Create(MessageType.JoinRequest, participantId,
            new JoinRequestPayload { ParticipantId = participantId, Username = "Player1" });

        var payload = msg.GetPayload<JoinRequestPayload>();
        Assert.NotNull(payload);
        Assert.Equal(participantId, payload.ParticipantId);
        Assert.Equal("Player1", payload.Username);
    }

    [Fact]
    public void Create_JoinResponse_WithParticipants_SerializesCorrectly()
    {
        var sessionId = Guid.NewGuid();
        var msg = NetworkMessage.Create(MessageType.JoinResponse, Guid.NewGuid(),
            new JoinResponsePayload
            {
                Accepted = true,
                SessionId = sessionId,
                AudioPort = 7778,
                Participants = new List<ParticipantInfo>
                {
                    new() { Id = Guid.NewGuid(), Username = "Host", IsHost = true },
                    new() { Id = Guid.NewGuid(), Username = "Player2", IsHost = false }
                }
            });

        var payload = msg.GetPayload<JoinResponsePayload>();
        Assert.NotNull(payload);
        Assert.True(payload.Accepted);
        Assert.Equal(sessionId, payload.SessionId);
        Assert.Equal(7778, payload.AudioPort);
        Assert.Equal(2, payload.Participants.Count);
        Assert.Equal("Host", payload.Participants[0].Username);
        Assert.True(payload.Participants[0].IsHost);
    }

    [Fact]
    public void Deserialize_InvalidJson_ReturnsNull()
    {
        var result = NetworkMessage.Deserialize("not valid json");
        Assert.Null(result);
    }

    [Fact]
    public void GetPayload_EmptyPayload_ReturnsDefault()
    {
        var msg = new NetworkMessage { Payload = "" };
        var result = msg.GetPayload<ChatPayload>();
        Assert.Null(result);
    }

    [Fact]
    public void AllMessageTypes_AreDistinct()
    {
        var types = Enum.GetValues<MessageType>();
        Assert.Equal(types.Length, types.Distinct().Count());
    }
}
