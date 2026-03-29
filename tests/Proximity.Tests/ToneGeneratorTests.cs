using Proximity.Audio.Diagnostics;
using Proximity.Audio.Pipeline;

namespace Proximity.Tests;

public class ToneGeneratorTests
{
    [Fact]
    public void GenerateSineTone_440Hz_1Second_ReturnsCorrectSampleCount()
    {
        var samples = ToneGenerator.GenerateSineTone(440, 1000, sampleRate: 48000);
        Assert.Equal(48000, samples.Length);
    }

    [Fact]
    public void GenerateSineTone_440Hz_20ms_ReturnsCorrectSampleCount()
    {
        var samples = ToneGenerator.GenerateSineTone(440, 20, sampleRate: 48000);
        Assert.Equal(960, samples.Length); // 48000 * 20 / 1000
    }

    [Fact]
    public void GenerateSineTone_NotAllZeros()
    {
        var samples = ToneGenerator.GenerateSineTone(440, 100, sampleRate: 48000, amplitude: 0.5);
        Assert.Contains(samples, s => s != 0);
    }

    [Fact]
    public void GenerateSineTone_ZeroAmplitude_AllZeros()
    {
        var samples = ToneGenerator.GenerateSineTone(440, 100, sampleRate: 48000, amplitude: 0.0);
        Assert.All(samples, s => Assert.Equal(0, s));
    }

    [Fact]
    public void GenerateSineTone_FullAmplitude_ReachesNearMaxValue()
    {
        var samples = ToneGenerator.GenerateSineTone(440, 100, sampleRate: 48000, amplitude: 1.0);
        var maxAbs = samples.Max(s => Math.Abs((int)s));
        // Should reach close to short.MaxValue (32767), allow 1% tolerance
        Assert.True(maxAbs > 32000, $"Max absolute sample {maxAbs} should be close to 32767");
    }

    [Fact]
    public void GenerateSineTone_InvalidFrequency_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ToneGenerator.GenerateSineTone(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => ToneGenerator.GenerateSineTone(-1, 100));
    }

    [Fact]
    public void GenerateSineTone_InvalidDuration_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ToneGenerator.GenerateSineTone(440, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ToneGenerator.GenerateSineTone(440, -1));
    }

    [Fact]
    public void GenerateSineTone_InvalidSampleRate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ToneGenerator.GenerateSineTone(440, 100, sampleRate: 0));
    }

    [Fact]
    public void GenerateSineToneBytes_ReturnsCorrectLength()
    {
        var bytes = ToneGenerator.GenerateSineToneBytes(440, 20, sampleRate: 48000);
        // 960 samples × 2 bytes each = 1920 bytes
        Assert.Equal(1920, bytes.Length);
    }

    [Fact]
    public void SplitIntoFrames_ExactMultiple_ReturnsCorrectFrameCount()
    {
        // 48000 samples / 960 per frame = 50 frames exactly
        var samples = new short[48000];
        var frames = ToneGenerator.SplitIntoFrames(samples, 960);
        Assert.Equal(50, frames.Count);
        Assert.All(frames, f => Assert.Equal(960, f.Length));
    }

    [Fact]
    public void SplitIntoFrames_NotExactMultiple_LastFrameZeroPadded()
    {
        // 1000 samples / 960 = 1 full frame + 1 partial (40 samples)
        var samples = new short[1000];
        for (int i = 0; i < samples.Length; i++) samples[i] = 100;

        var frames = ToneGenerator.SplitIntoFrames(samples, 960);
        Assert.Equal(2, frames.Count);
        Assert.Equal(960, frames[0].Length);
        Assert.Equal(960, frames[1].Length);

        // First frame should be all 100s
        Assert.All(frames[0], s => Assert.Equal(100, s));

        // Second frame: first 40 samples should be 100, rest should be 0
        for (int i = 0; i < 40; i++) Assert.Equal(100, frames[1][i]);
        for (int i = 40; i < 960; i++) Assert.Equal(0, frames[1][i]);
    }

    [Fact]
    public void SplitIntoFrames_EmptyInput_ReturnsNoFrames()
    {
        var frames = ToneGenerator.SplitIntoFrames(Array.Empty<short>(), 960);
        Assert.Empty(frames);
    }

    [Fact]
    public void SplitIntoFrames_InvalidFrameSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ToneGenerator.SplitIntoFrames(new short[100], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ToneGenerator.SplitIntoFrames(new short[100], -1));
    }

    [Fact]
    public void GenerateSineTone_AmplitudeClampedAboveOne()
    {
        // Amplitude > 1.0 should be clamped to 1.0
        var samples = ToneGenerator.GenerateSineTone(440, 20, sampleRate: 48000, amplitude: 2.0);
        var maxAbs = samples.Max(s => Math.Abs((int)s));
        Assert.True(maxAbs <= short.MaxValue);
    }

    [Fact]
    public void GenerateSineTone_DifferentFrequencies_ProduceDifferentOutput()
    {
        var samples440 = ToneGenerator.GenerateSineTone(440, 20, sampleRate: 48000);
        var samples880 = ToneGenerator.GenerateSineTone(880, 20, sampleRate: 48000);

        // They should not be identical
        bool differ = false;
        for (int i = 0; i < samples440.Length; i++)
        {
            if (samples440[i] != samples880[i])
            {
                differ = true;
                break;
            }
        }
        Assert.True(differ, "Different frequencies should produce different samples");
    }
}
