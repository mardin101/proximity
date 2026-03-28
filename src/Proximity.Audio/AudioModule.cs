using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;
using Proximity.Core.Models;

namespace Proximity.Audio;

/// <summary>
/// Audio module for handling audio capture, playback, and per-user volume control.
/// Uses platform audio APIs (NAudio on Windows) for real-time audio processing.
/// </summary>
public class AudioModule : IModule
{
    private readonly ILogger<AudioModule> _logger;
    private readonly IAudioDeviceEnumerator _deviceEnumerator;

    public string ModuleName => "Audio";

    /// <summary>
    /// Whether audio capture is currently active
    /// </summary>
    public bool IsCapturing { get; private set; }

    /// <summary>
    /// Whether the local user is muted
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// Currently selected input (microphone) device, or null for system default
    /// </summary>
    public AudioDevice? SelectedInputDevice { get; private set; }

    /// <summary>
    /// Currently selected output (speaker) device, or null for system default
    /// </summary>
    public AudioDevice? SelectedOutputDevice { get; private set; }

    /// <summary>
    /// Per-participant volume levels (0.0 to 1.0)
    /// </summary>
    private readonly Dictionary<Guid, float> _participantVolumes = new();
    private readonly object _volumeLock = new();

    public AudioModule(ILogger<AudioModule> logger, IAudioDeviceEnumerator deviceEnumerator)
    {
        _logger = logger;
        _deviceEnumerator = deviceEnumerator;
    }

    public Task InitializeAsync()
    {
        _logger.LogInformation("Audio module initializing...");

        // Set default devices on initialization
        SelectedInputDevice = _deviceEnumerator.GetDefaultInputDevice();
        SelectedOutputDevice = _deviceEnumerator.GetDefaultOutputDevice();

        _logger.LogInformation("Default input device: {InputDevice}", SelectedInputDevice?.Name ?? "None");
        _logger.LogInformation("Default output device: {OutputDevice}", SelectedOutputDevice?.Name ?? "None");
        _logger.LogInformation("Audio module initialized (platform audio will be configured on session join)");
        return Task.CompletedTask;
    }

    public Task StartAsync()
    {
        _logger.LogInformation("Audio module started");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get all available audio input (microphone) devices
    /// </summary>
    public IReadOnlyList<AudioDevice> GetInputDevices()
    {
        return _deviceEnumerator.GetInputDevices();
    }

    /// <summary>
    /// Get all available audio output (speaker) devices
    /// </summary>
    public IReadOnlyList<AudioDevice> GetOutputDevices()
    {
        return _deviceEnumerator.GetOutputDevices();
    }

    /// <summary>
    /// Select an input (microphone) device by its ID
    /// </summary>
    public bool SetInputDevice(AudioDevice? device)
    {
        if (device != null && !device.IsInput)
        {
            _logger.LogWarning("Attempted to set non-input device '{DeviceName}' as input", device.Name);
            return false;
        }

        SelectedInputDevice = device;
        _logger.LogInformation("Input device changed to: {DeviceName}", device?.Name ?? "System Default");
        return true;
    }

    /// <summary>
    /// Select an output (speaker) device by its ID
    /// </summary>
    public bool SetOutputDevice(AudioDevice? device)
    {
        if (device != null && !device.IsOutput)
        {
            _logger.LogWarning("Attempted to set non-output device '{DeviceName}' as output", device.Name);
            return false;
        }

        SelectedOutputDevice = device;
        _logger.LogInformation("Output device changed to: {DeviceName}", device?.Name ?? "System Default");
        return true;
    }

    /// <summary>
    /// Set the volume for a specific participant (local-only control)
    /// </summary>
    public void SetParticipantVolume(Guid participantId, float volume)
    {
        volume = Math.Clamp(volume, 0.0f, 1.0f);
        lock (_volumeLock)
        {
            _participantVolumes[participantId] = volume;
        }
        _logger.LogDebug("Set volume for {ParticipantId} to {Volume:F2}", participantId, volume);
    }

    /// <summary>
    /// Get the volume for a specific participant
    /// </summary>
    public float GetParticipantVolume(Guid participantId)
    {
        lock (_volumeLock)
        {
            return _participantVolumes.TryGetValue(participantId, out var volume) ? volume : 1.0f;
        }
    }

    /// <summary>
    /// Remove volume tracking for a participant
    /// </summary>
    public void RemoveParticipant(Guid participantId)
    {
        lock (_volumeLock)
        {
            _participantVolumes.Remove(participantId);
        }
    }

    public Task StopAsync()
    {
        _logger.LogInformation("Audio module stopping...");
        IsCapturing = false;
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _logger.LogInformation("Audio module disposing...");
        lock (_volumeLock)
        {
            _participantVolumes.Clear();
        }
        return Task.CompletedTask;
    }
}
