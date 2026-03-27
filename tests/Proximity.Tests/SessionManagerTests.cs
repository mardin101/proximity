using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Network.Session;

namespace Proximity.Tests;

public class SessionManagerTests
{
    [Fact]
    public async Task CreateSession_SetsUpHostState()
    {
        var manager = new SessionManager(NullLogger<SessionManager>.Instance);

        var session = await manager.CreateSessionAsync("Test Session", "HostUser", 17777);

        Assert.NotNull(session);
        Assert.Equal("Test Session", session.SessionName);
        Assert.Equal("HostUser", session.HostName);
        Assert.Equal(17777, session.Port);
        Assert.Equal(1, session.ParticipantCount);
        Assert.True(manager.IsHost);
        Assert.True(manager.IsConnected);
        Assert.NotEqual(Guid.Empty, manager.LocalParticipantId);

        // Clean up
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task CreateSession_ThenLeave_CleansUp()
    {
        var manager = new SessionManager(NullLogger<SessionManager>.Instance);

        await manager.CreateSessionAsync("Test Session", "HostUser", 17778);
        Assert.True(manager.IsConnected);

        await manager.LeaveSessionAsync();
        Assert.False(manager.IsConnected);
        Assert.Null(manager.CurrentSession);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task JoinSession_FailsWithInvalidAddress()
    {
        var manager = new SessionManager(NullLogger<SessionManager>.Instance);

        // Use localhost with an unused port for immediate connection refusal
        var session = new Core.Models.VoiceSession
        {
            HostAddress = "127.0.0.1",
            Port = 1,
            SessionName = "Unreachable"
        };

        var result = await manager.JoinSessionAsync(session, "TestUser");

        Assert.False(result);
        Assert.False(manager.IsConnected);

        await manager.DisposeAsync();
    }

    [Fact]
    public void KickParticipant_AsNonHost_DoesNotThrow()
    {
        var manager = new SessionManager(NullLogger<SessionManager>.Instance);

        // Not a host, not connected - should log warning but not throw
        var task = manager.KickParticipantAsync(Guid.NewGuid(), "test");
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public async Task SendMuteState_WhenNotConnected_DoesNotThrow()
    {
        var manager = new SessionManager(NullLogger<SessionManager>.Instance);

        // Should not throw when not connected
        await manager.SendMuteStateAsync(true);

        await manager.DisposeAsync();
    }
}
