using Proximity.Core.Models;

namespace Proximity.Tests;

public class AudioDeviceTests
{
    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        var device1 = new AudioDevice { Id = "device-1", Name = "Device One" };
        var device2 = new AudioDevice { Id = "device-1", Name = "Device One Copy" };

        Assert.Equal(device1, device2);
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var device1 = new AudioDevice { Id = "device-1", Name = "Device" };
        var device2 = new AudioDevice { Id = "device-2", Name = "Device" };

        Assert.NotEqual(device1, device2);
    }

    [Fact]
    public void GetHashCode_SameId_ReturnsSameHash()
    {
        var device1 = new AudioDevice { Id = "device-1", Name = "A" };
        var device2 = new AudioDevice { Id = "device-1", Name = "B" };

        Assert.Equal(device1.GetHashCode(), device2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        var device = new AudioDevice { Id = "id", Name = "My Microphone" };

        Assert.Equal("My Microphone", device.ToString());
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var device = new AudioDevice();

        Assert.Equal(string.Empty, device.Id);
        Assert.Equal(string.Empty, device.Name);
        Assert.False(device.IsDefault);
    }
}
