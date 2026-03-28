using Proximity.Core.Models;

namespace Proximity.Core.Interfaces;

/// <summary>
/// Interface for enumerating available audio devices on the host machine
/// </summary>
public interface IAudioDeviceEnumerator
{
    /// <summary>
    /// Get all available audio input (capture/microphone) devices
    /// </summary>
    IReadOnlyList<AudioDevice> GetInputDevices();

    /// <summary>
    /// Get all available audio output (playback/speaker) devices
    /// </summary>
    IReadOnlyList<AudioDevice> GetOutputDevices();

    /// <summary>
    /// Get the system default input device, or null if none available
    /// </summary>
    AudioDevice? GetDefaultInputDevice();

    /// <summary>
    /// Get the system default output device, or null if none available
    /// </summary>
    AudioDevice? GetDefaultOutputDevice();
}
