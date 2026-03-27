using Proximity.Core.Models;

namespace Proximity.Tests;

public class ChatMessageTests
{
    [Fact]
    public void NewChatMessage_HasDefaultValues()
    {
        var msg = new ChatMessage();

        Assert.NotEqual(Guid.Empty, msg.MessageId);
        Assert.Equal(string.Empty, msg.SenderName);
        Assert.Equal(string.Empty, msg.Content);
        Assert.Equal(Guid.Empty, msg.SenderId);
    }

    [Fact]
    public void ChatMessage_CanSetProperties()
    {
        var senderId = Guid.NewGuid();
        var msg = new ChatMessage
        {
            SenderId = senderId,
            SenderName = "TestUser",
            Content = "Hello, world!",
            Timestamp = new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        Assert.Equal(senderId, msg.SenderId);
        Assert.Equal("TestUser", msg.SenderName);
        Assert.Equal("Hello, world!", msg.Content);
        Assert.Equal(new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc), msg.Timestamp);
    }
}
