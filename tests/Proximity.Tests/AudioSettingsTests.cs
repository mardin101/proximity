using Proximity.Core.Configuration;

namespace Proximity.Tests;

public class AudioSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var settings = new AudioSettings();

        Assert.Equal(48000, settings.SampleRate);
        Assert.Equal(1024, settings.BufferSize);
        Assert.Equal(1, settings.Channels);
        Assert.Equal(20, settings.FrameSizeMs);
        Assert.Equal(32000, settings.Bitrate);
        Assert.Equal(60, settings.JitterBufferMs);
    }

    [Fact]
    public void FrameSizeSamples_ComputedCorrectly()
    {
        var settings = new AudioSettings();

        // 48000 * 20 / 1000 = 960 samples
        Assert.Equal(960, settings.FrameSizeSamples);
    }

    [Fact]
    public void FrameSizeSamples_UpdatesWithSampleRate()
    {
        var settings = new AudioSettings { SampleRate = 24000 };

        // 24000 * 20 / 1000 = 480 samples
        Assert.Equal(480, settings.FrameSizeSamples);
    }

    [Fact]
    public void FrameSizeSamples_UpdatesWithFrameSizeMs()
    {
        var settings = new AudioSettings { FrameSizeMs = 40 };

        // 48000 * 40 / 1000 = 1920 samples
        Assert.Equal(1920, settings.FrameSizeSamples);
    }
}
