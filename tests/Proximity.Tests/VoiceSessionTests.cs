using Proximity.Core.Models;

namespace Proximity.Tests;

public class VoiceSessionTests
{
    [Fact]
    public void NewSession_HasDefaultValues()
    {
        var session = new VoiceSession();

        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.Equal(string.Empty, session.SessionName);
        Assert.Equal(string.Empty, session.HostName);
        Assert.Equal(string.Empty, session.HostAddress);
        Assert.Equal(0, session.Port);
        Assert.Equal(0, session.ParticipantCount);
        Assert.Equal(10, session.MaxParticipants);
    }

    [Fact]
    public void Session_CanSetProperties()
    {
        var sessionId = Guid.NewGuid();
        var session = new VoiceSession
        {
            SessionId = sessionId,
            SessionName = "Game Night",
            HostName = "Player1",
            HostAddress = "192.168.1.50",
            Port = 7777,
            ParticipantCount = 3,
            MaxParticipants = 8
        };

        Assert.Equal(sessionId, session.SessionId);
        Assert.Equal("Game Night", session.SessionName);
        Assert.Equal("Player1", session.HostName);
        Assert.Equal("192.168.1.50", session.HostAddress);
        Assert.Equal(7777, session.Port);
        Assert.Equal(3, session.ParticipantCount);
        Assert.Equal(8, session.MaxParticipants);
    }
}
