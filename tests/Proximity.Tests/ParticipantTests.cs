using Proximity.Core.Models;

namespace Proximity.Tests;

public class ParticipantTests
{
    [Fact]
    public void NewParticipant_HasDefaultValues()
    {
        var participant = new Participant();

        Assert.NotEqual(Guid.Empty, participant.Id);
        Assert.Equal(string.Empty, participant.Username);
        Assert.False(participant.IsHost);
        Assert.False(participant.IsMuted);
        Assert.Equal(1.0f, participant.Volume);
        Assert.False(participant.IsLocallyMuted);
        Assert.False(participant.IsSpeaking);
    }

    [Fact]
    public void Participant_CanSetProperties()
    {
        var id = Guid.NewGuid();
        var participant = new Participant
        {
            Id = id,
            Username = "TestUser",
            IsHost = true,
            IsMuted = true,
            Volume = 0.5f,
            IsLocallyMuted = true,
            IsSpeaking = true,
            EndPoint = "192.168.1.100"
        };

        Assert.Equal(id, participant.Id);
        Assert.Equal("TestUser", participant.Username);
        Assert.True(participant.IsHost);
        Assert.True(participant.IsMuted);
        Assert.Equal(0.5f, participant.Volume);
        Assert.True(participant.IsLocallyMuted);
        Assert.True(participant.IsSpeaking);
        Assert.Equal("192.168.1.100", participant.EndPoint);
    }
}
