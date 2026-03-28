using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Audio;
using Proximity.Core.Models;

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

    [Fact]
    public void RefreshDevices_PopulatesInputAndOutputDevices()
    {
        _audioModule.RefreshDevices();

        Assert.NotEmpty(_audioModule.InputDevices);
        Assert.NotEmpty(_audioModule.OutputDevices);
    }

    [Fact]
    public void RefreshDevices_SetsDefaultSelectedDevices()
    {
        _audioModule.RefreshDevices();

        Assert.NotNull(_audioModule.SelectedInputDevice);
        Assert.NotNull(_audioModule.SelectedOutputDevice);
    }

    [Fact]
    public void SelectInputDevice_UpdatesSelectedDevice()
    {
        _audioModule.RefreshDevices();

        var device = new AudioDevice { Id = "test-mic", Name = "Test Microphone" };
        _audioModule.SelectInputDevice(device);

        Assert.Equal("test-mic", _audioModule.SelectedInputDevice?.Id);
    }

    [Fact]
    public void SelectOutputDevice_UpdatesSelectedDevice()
    {
        _audioModule.RefreshDevices();

        var device = new AudioDevice { Id = "test-speaker", Name = "Test Speaker" };
        _audioModule.SelectOutputDevice(device);

        Assert.Equal("test-speaker", _audioModule.SelectedOutputDevice?.Id);
    }

    [Fact]
    public async Task InitializeAsync_RefreshesDevices()
    {
        await _audioModule.InitializeAsync();

        Assert.NotEmpty(_audioModule.InputDevices);
        Assert.NotEmpty(_audioModule.OutputDevices);
        Assert.NotNull(_audioModule.SelectedInputDevice);
        Assert.NotNull(_audioModule.SelectedOutputDevice);
    }

    [Fact]
    public void RefreshDevices_PreservesExistingSelection()
    {
        _audioModule.RefreshDevices();

        var customDevice = new AudioDevice { Id = "custom", Name = "Custom Device" };
        _audioModule.SelectInputDevice(customDevice);

        _audioModule.RefreshDevices();

        // After refresh, the selection should be preserved (not overwritten)
        Assert.Equal("custom", _audioModule.SelectedInputDevice?.Id);
    }

    [Fact]
    public void VoiceFeedbackLevel_DefaultIsZero()
    {
        Assert.Equal(0, _audioModule.VoiceFeedbackLevel);
    }

    [Fact]
    public void VoiceFeedbackLevel_CanBeSet()
    {
        _audioModule.VoiceFeedbackLevel = 50;
        Assert.Equal(50, _audioModule.VoiceFeedbackLevel);
    }

    [Fact]
    public void VoiceFeedbackLevel_ClampsToRange()
    {
        _audioModule.VoiceFeedbackLevel = 150;
        Assert.Equal(100, _audioModule.VoiceFeedbackLevel);

        _audioModule.VoiceFeedbackLevel = -10;
        Assert.Equal(0, _audioModule.VoiceFeedbackLevel);
    }

    [Fact]
    public void VoiceFeedbackLevel_AcceptsBoundaryValues()
    {
        _audioModule.VoiceFeedbackLevel = 0;
        Assert.Equal(0, _audioModule.VoiceFeedbackLevel);

        _audioModule.VoiceFeedbackLevel = 100;
        Assert.Equal(100, _audioModule.VoiceFeedbackLevel);
    }
}
