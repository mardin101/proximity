using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Audio;
using Proximity.Core.Interfaces;
using Proximity.Core.Models;

namespace Proximity.Tests;

/// <summary>
/// Stub implementation of IAudioDeviceEnumerator for testing
/// </summary>
internal class StubAudioDeviceEnumerator : IAudioDeviceEnumerator
{
    private readonly List<AudioDevice> _inputDevices;
    private readonly List<AudioDevice> _outputDevices;

    public StubAudioDeviceEnumerator(
        List<AudioDevice>? inputDevices = null,
        List<AudioDevice>? outputDevices = null)
    {
        _inputDevices = inputDevices ?? new List<AudioDevice>
        {
            new("0", "Test Microphone", isInput: true, isOutput: false),
            new("1", "Test Headset Mic", isInput: true, isOutput: false)
        };
        _outputDevices = outputDevices ?? new List<AudioDevice>
        {
            new("0", "Test Speakers", isInput: false, isOutput: true),
            new("1", "Test Headphones", isInput: false, isOutput: true)
        };
    }

    public IReadOnlyList<AudioDevice> GetInputDevices() => _inputDevices;
    public IReadOnlyList<AudioDevice> GetOutputDevices() => _outputDevices;
    public AudioDevice? GetDefaultInputDevice() => _inputDevices.Count > 0 ? _inputDevices[0] : null;
    public AudioDevice? GetDefaultOutputDevice() => _outputDevices.Count > 0 ? _outputDevices[0] : null;
}

public class AudioModuleTests
{
    private readonly AudioModule _audioModule;
    private readonly StubAudioDeviceEnumerator _enumerator;

    public AudioModuleTests()
    {
        _enumerator = new StubAudioDeviceEnumerator();
        _audioModule = new AudioModule(NullLogger<AudioModule>.Instance, _enumerator);
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

    // --- Audio device selection tests ---

    [Fact]
    public async Task InitializeAsync_SetsDefaultDevices()
    {
        await _audioModule.InitializeAsync();

        Assert.NotNull(_audioModule.SelectedInputDevice);
        Assert.Equal("Test Microphone", _audioModule.SelectedInputDevice!.Name);
        Assert.NotNull(_audioModule.SelectedOutputDevice);
        Assert.Equal("Test Speakers", _audioModule.SelectedOutputDevice!.Name);
    }

    [Fact]
    public void GetInputDevices_ReturnsEnumeratedDevices()
    {
        var devices = _audioModule.GetInputDevices();

        Assert.Equal(2, devices.Count);
        Assert.Equal("Test Microphone", devices[0].Name);
        Assert.Equal("Test Headset Mic", devices[1].Name);
        Assert.All(devices, d => Assert.True(d.IsInput));
    }

    [Fact]
    public void GetOutputDevices_ReturnsEnumeratedDevices()
    {
        var devices = _audioModule.GetOutputDevices();

        Assert.Equal(2, devices.Count);
        Assert.Equal("Test Speakers", devices[0].Name);
        Assert.Equal("Test Headphones", devices[1].Name);
        Assert.All(devices, d => Assert.True(d.IsOutput));
    }

    [Fact]
    public void SetInputDevice_AcceptsValidInputDevice()
    {
        var device = new AudioDevice("1", "Test Headset Mic", isInput: true, isOutput: false);
        var result = _audioModule.SetInputDevice(device);

        Assert.True(result);
        Assert.Equal(device, _audioModule.SelectedInputDevice);
    }

    [Fact]
    public void SetInputDevice_RejectsNonInputDevice()
    {
        var outputDevice = new AudioDevice("0", "Test Speakers", isInput: false, isOutput: true);
        var result = _audioModule.SetInputDevice(outputDevice);

        Assert.False(result);
    }

    [Fact]
    public void SetOutputDevice_AcceptsValidOutputDevice()
    {
        var device = new AudioDevice("1", "Test Headphones", isInput: false, isOutput: true);
        var result = _audioModule.SetOutputDevice(device);

        Assert.True(result);
        Assert.Equal(device, _audioModule.SelectedOutputDevice);
    }

    [Fact]
    public void SetOutputDevice_RejectsNonOutputDevice()
    {
        var inputDevice = new AudioDevice("0", "Test Microphone", isInput: true, isOutput: false);
        var result = _audioModule.SetOutputDevice(inputDevice);

        Assert.False(result);
    }

    [Fact]
    public void SetInputDevice_AcceptsNull()
    {
        var result = _audioModule.SetInputDevice(null);

        Assert.True(result);
        Assert.Null(_audioModule.SelectedInputDevice);
    }

    [Fact]
    public void SetOutputDevice_AcceptsNull()
    {
        var result = _audioModule.SetOutputDevice(null);

        Assert.True(result);
        Assert.Null(_audioModule.SelectedOutputDevice);
    }

    [Fact]
    public void GetInputDevices_ReturnsEmpty_WhenNoDevicesAvailable()
    {
        var emptyEnumerator = new StubAudioDeviceEnumerator(
            new List<AudioDevice>(), new List<AudioDevice>());
        var module = new AudioModule(NullLogger<AudioModule>.Instance, emptyEnumerator);

        var devices = module.GetInputDevices();

        Assert.Empty(devices);
    }

    [Fact]
    public void GetOutputDevices_ReturnsEmpty_WhenNoDevicesAvailable()
    {
        var emptyEnumerator = new StubAudioDeviceEnumerator(
            new List<AudioDevice>(), new List<AudioDevice>());
        var module = new AudioModule(NullLogger<AudioModule>.Instance, emptyEnumerator);

        var devices = module.GetOutputDevices();

        Assert.Empty(devices);
    }
}
