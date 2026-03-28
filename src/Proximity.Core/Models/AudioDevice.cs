namespace Proximity.Core.Models;

/// <summary>
/// Represents an audio device available on the host machine
/// </summary>
public class AudioDevice
{
    /// <summary>
    /// Platform-specific device identifier
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Human-readable device name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether this is an input (capture) device
    /// </summary>
    public bool IsInput { get; }

    /// <summary>
    /// Whether this is an output (playback) device
    /// </summary>
    public bool IsOutput { get; }

    public AudioDevice(string id, string name, bool isInput, bool isOutput)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsInput = isInput;
        IsOutput = isOutput;
    }

    public override bool Equals(object? obj)
    {
        return obj is AudioDevice other && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString() => Name;
}
