using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Audio;

namespace Proximity.Tests;

public class AudioModuleTests
{
    private readonly AudioModule _audioModule;

    public AudioModuleTests()
    {
        _audioModule = new AudioModule(NullLogger<AudioModule>.Instance);
    }

    [Fact]
    public async Task InitializeAsync_Succeeds()
    {
        await _audioModule.InitializeAsync();
        // Should not throw
    }

    [Fact]
    public async Task StartAsync_Succeeds()
    {
        await _audioModule.InitializeAsync();
        await _audioModule.StartAsync();
        // Should not throw
    }

    [Fact]
    public async Task StopAsync_SetsIsCapturingToFalse()
    {
        await _audioModule.InitializeAsync();
        await _audioModule.StartAsync();
        await _audioModule.StopAsync();

        Assert.False(_audioModule.IsCapturing);
    }

    [Fact]
    public void SetParticipantVolume_ClampsToRange()
    {
        var id = Guid.NewGuid();

        _audioModule.SetParticipantVolume(id, 1.5f);
        Assert.Equal(1.0f, _audioModule.GetParticipantVolume(id));

        _audioModule.SetParticipantVolume(id, -0.5f);
        Assert.Equal(0.0f, _audioModule.GetParticipantVolume(id));

        _audioModule.SetParticipantVolume(id, 0.75f);
        Assert.Equal(0.75f, _audioModule.GetParticipantVolume(id));
    }

    [Fact]
    public void GetParticipantVolume_ReturnsDefault_ForUnknownParticipant()
    {
        var volume = _audioModule.GetParticipantVolume(Guid.NewGuid());
        Assert.Equal(1.0f, volume);
    }

    [Fact]
    public void RemoveParticipant_RemovesVolumeTracking()
    {
        var id = Guid.NewGuid();
        _audioModule.SetParticipantVolume(id, 0.5f);
        Assert.Equal(0.5f, _audioModule.GetParticipantVolume(id));

        _audioModule.RemoveParticipant(id);
        Assert.Equal(1.0f, _audioModule.GetParticipantVolume(id)); // Returns default
    }

    [Fact]
    public void ModuleName_IsAudio()
    {
        Assert.Equal("Audio", _audioModule.ModuleName);
    }

    [Fact]
    public void IsMuted_DefaultIsFalse()
    {
        Assert.False(_audioModule.IsMuted);
    }

    [Fact]
    public void IsMuted_CanBeToggled()
    {
        _audioModule.IsMuted = true;
        Assert.True(_audioModule.IsMuted);

        _audioModule.IsMuted = false;
        Assert.False(_audioModule.IsMuted);
    }
}
