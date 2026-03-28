using Proximity.Core.Models;

namespace Proximity.Tests;

public class AudioDeviceTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var device = new AudioDevice("dev-1", "Test Mic", isInput: true, isOutput: false);

        Assert.Equal("dev-1", device.Id);
        Assert.Equal("Test Mic", device.Name);
        Assert.True(device.IsInput);
        Assert.False(device.IsOutput);
    }

    [Fact]
    public void Constructor_ThrowsOnNullId()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AudioDevice(null!, "Test", isInput: true, isOutput: false));
    }

    [Fact]
    public void Constructor_ThrowsOnNullName()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AudioDevice("1", null!, isInput: true, isOutput: false));
    }

    [Fact]
    public void Equals_ReturnsTrueForSameId()
    {
        var device1 = new AudioDevice("1", "Mic A", isInput: true, isOutput: false);
        var device2 = new AudioDevice("1", "Mic B", isInput: true, isOutput: false);

        Assert.Equal(device1, device2);
    }

    [Fact]
    public void Equals_ReturnsFalseForDifferentId()
    {
        var device1 = new AudioDevice("1", "Mic", isInput: true, isOutput: false);
        var device2 = new AudioDevice("2", "Mic", isInput: true, isOutput: false);

        Assert.NotEqual(device1, device2);
    }

    [Fact]
    public void GetHashCode_SameForSameId()
    {
        var device1 = new AudioDevice("1", "Mic A", isInput: true, isOutput: false);
        var device2 = new AudioDevice("1", "Mic B", isInput: true, isOutput: false);

        Assert.Equal(device1.GetHashCode(), device2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        var device = new AudioDevice("1", "My Microphone", isInput: true, isOutput: false);

        Assert.Equal("My Microphone", device.ToString());
    }

    [Fact]
    public void CanCreateInputDevice()
    {
        var device = new AudioDevice("0", "Input Device", isInput: true, isOutput: false);

        Assert.True(device.IsInput);
        Assert.False(device.IsOutput);
    }

    [Fact]
    public void CanCreateOutputDevice()
    {
        var device = new AudioDevice("0", "Output Device", isInput: false, isOutput: true);

        Assert.False(device.IsInput);
        Assert.True(device.IsOutput);
    }
}
