namespace Proximity.Core.Models;

/// <summary>
/// Represents an audio device (input or output)
/// </summary>
public class AudioDevice : IEquatable<AudioDevice>
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

    public bool Equals(AudioDevice? other)
    {
        return other is not null && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as AudioDevice);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(AudioDevice? left, AudioDevice? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(AudioDevice? left, AudioDevice? right)
    {
        return !(left == right);
    }

    public override string ToString() => Name;
}
