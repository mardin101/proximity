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
    /// Percentage of incoming voice volume played back to the user's output device (0 to 100)
    /// </summary>
    public int VoiceFeedbackLevel
    {
        get => _voiceFeedbackLevel;
        set
        {
            _voiceFeedbackLevel = Math.Clamp(value, 0, 100);
            _logger.LogDebug("Voice feedback level set to {Level}%", _voiceFeedbackLevel);
        }
    }
    private int _voiceFeedbackLevel = 0;

    /// <summary>
    /// Available input (microphone) devices
    /// </summary>
    public IReadOnlyList<AudioDevice> InputDevices { get; private set; } = Array.Empty<AudioDevice>();

    /// <summary>
    /// Available output (speaker) devices
    /// </summary>
    public IReadOnlyList<AudioDevice> OutputDevices { get; private set; } = Array.Empty<AudioDevice>();

    /// <summary>
    /// Currently selected input device
    /// </summary>
    public AudioDevice? SelectedInputDevice { get; private set; }

    /// <summary>
    /// Currently selected output device
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
        RefreshDevices();
        _logger.LogInformation("Audio module initialized (platform audio will be configured on session join)");
        return Task.CompletedTask;
    }

    public Task StartAsync()
    {
        _logger.LogInformation("Audio module started");
        return Task.CompletedTask;
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

    /// <summary>
    /// Refresh the list of available audio devices
    /// </summary>
    public void RefreshDevices()
    {
        InputDevices = _deviceEnumerator.GetInputDevices();
        OutputDevices = _deviceEnumerator.GetOutputDevices();

        // Auto-select defaults if nothing is selected
        if (SelectedInputDevice is null && InputDevices.Count > 0)
        {
            SelectedInputDevice = InputDevices.FirstOrDefault(d => d.IsDefault) ?? InputDevices[0];
        }

        if (SelectedOutputDevice is null && OutputDevices.Count > 0)
        {
            SelectedOutputDevice = OutputDevices.FirstOrDefault(d => d.IsDefault) ?? OutputDevices[0];
        }

        _logger.LogInformation("Refreshed audio devices: {InputCount} input(s), {OutputCount} output(s)",
            InputDevices.Count, OutputDevices.Count);
    }

    /// <summary>
    /// Select an input (microphone) device
    /// </summary>
    public void SelectInputDevice(AudioDevice device)
    {
        SelectedInputDevice = device;
        _logger.LogInformation("Selected input device: {DeviceName} ({DeviceId})", device.Name, device.Id);
    }

    /// <summary>
    /// Select an output (speaker) device
    /// </summary>
    public void SelectOutputDevice(AudioDevice device)
    {
        SelectedOutputDevice = device;
        _logger.LogInformation("Selected output device: {DeviceName} ({DeviceId})", device.Name, device.Id);
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
