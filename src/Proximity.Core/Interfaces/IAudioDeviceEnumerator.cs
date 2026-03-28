using Proximity.Core.Models;

namespace Proximity.Core.Interfaces;

/// <summary>
/// Interface for enumerating available audio devices on the host machine
/// </summary>
public interface IAudioDeviceEnumerator
{
    /// <summary>
    /// Get all available audio input (microphone/recording) devices
    /// </summary>
    IReadOnlyList<AudioDevice> GetInputDevices();

    /// <summary>
    /// Get all available audio output (speaker/playback) devices
    /// </summary>
    IReadOnlyList<AudioDevice> GetOutputDevices();
}
