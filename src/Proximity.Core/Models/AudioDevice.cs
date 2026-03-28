namespace Proximity.Core.Models;

/// <summary>
/// Represents an audio device (input or output)
/// </summary>
public class AudioDevice
{
    /// <summary>
    /// Unique identifier for the device
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the device
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the system default device
    /// </summary>
    public bool IsDefault { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is AudioDevice device && Id == device.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString() => Name;
}
