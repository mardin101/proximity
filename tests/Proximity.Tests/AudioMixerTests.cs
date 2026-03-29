using Proximity.Audio.Pipeline;

namespace Proximity.Tests;

public class AudioMixerTests
{
    [Fact]
    public void SetVolume_ClampsToRange()
    {
        var mixer = new AudioMixer();
        var id = Guid.NewGuid();

        mixer.SetVolume(id, 1.5f);
        Assert.Equal(1.0f, mixer.GetVolume(id));

        mixer.SetVolume(id, -0.5f);
        Assert.Equal(0.0f, mixer.GetVolume(id));

        mixer.SetVolume(id, 0.75f);
        Assert.Equal(0.75f, mixer.GetVolume(id));
    }

    [Fact]
    public void GetVolume_ReturnsDefault_ForUnknown()
    {
        var mixer = new AudioMixer();
        Assert.Equal(1.0f, mixer.GetVolume(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveParticipant_ResetsVolume()
    {
        var mixer = new AudioMixer();
        var id = Guid.NewGuid();

        mixer.SetVolume(id, 0.5f);
        mixer.RemoveParticipant(id);

        Assert.Equal(1.0f, mixer.GetVolume(id)); // Returns default after removal
    }

    [Fact]
    public void Mix_EmptyStreams_ReturnsSilence()
    {
        var mixer = new AudioMixer();
        var streams = new Dictionary<Guid, short[]>();

        var result = mixer.Mix(streams, 960);

        Assert.Equal(960, result.Length);
        Assert.All(result, s => Assert.Equal(0, s));
    }

    [Fact]
    public void Mix_SingleStream_ReturnsWithVolume()
    {
        var mixer = new AudioMixer();
        var id = Guid.NewGuid();
        var samples = new short[] { 1000, -1000, 500, -500 };

        var streams = new Dictionary<Guid, short[]> { { id, samples } };
        var result = mixer.Mix(streams, 4);

        // Default volume is 1.0, so values should be unchanged
        Assert.Equal(1000, result[0]);
        Assert.Equal(-1000, result[1]);
        Assert.Equal(500, result[2]);
        Assert.Equal(-500, result[3]);
    }

    [Fact]
    public void Mix_SingleStream_AppliesVolume()
    {
        var mixer = new AudioMixer();
        var id = Guid.NewGuid();
        mixer.SetVolume(id, 0.5f);

        var samples = new short[] { 1000, -1000, 500, -500 };
        var streams = new Dictionary<Guid, short[]> { { id, samples } };
        var result = mixer.Mix(streams, 4);

        Assert.Equal(500, result[0]);
        Assert.Equal(-500, result[1]);
        Assert.Equal(250, result[2]);
        Assert.Equal(-250, result[3]);
    }

    [Fact]
    public void Mix_MultipleStreams_SumsCorrectly()
    {
        var mixer = new AudioMixer();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var samples1 = new short[] { 1000, 2000 };
        var samples2 = new short[] { 500, 1000 };

        var streams = new Dictionary<Guid, short[]>
        {
            { id1, samples1 },
            { id2, samples2 }
        };

        var result = mixer.Mix(streams, 2);

        Assert.Equal(1500, result[0]);
        Assert.Equal(3000, result[1]);
    }

    [Fact]
    public void Mix_MultipleStreams_ClipsToPreventOverflow()
    {
        var mixer = new AudioMixer();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var samples1 = new short[] { 30000 };
        var samples2 = new short[] { 20000 };

        var streams = new Dictionary<Guid, short[]>
        {
            { id1, samples1 },
            { id2, samples2 }
        };

        var result = mixer.Mix(streams, 1);

        // 30000 + 20000 = 50000, but should be clipped to short.MaxValue (32767)
        Assert.Equal(short.MaxValue, result[0]);
    }

    [Fact]
    public void Mix_MultipleStreams_ClipsNegativeOverflow()
    {
        var mixer = new AudioMixer();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var samples1 = new short[] { -30000 };
        var samples2 = new short[] { -20000 };

        var streams = new Dictionary<Guid, short[]>
        {
            { id1, samples1 },
            { id2, samples2 }
        };

        var result = mixer.Mix(streams, 1);

        // -30000 + -20000 = -50000, clipped to short.MinValue (-32768)
        Assert.Equal(short.MinValue, result[0]);
    }

    [Fact]
    public void Mix_MultipleStreams_AppliesPerParticipantVolume()
    {
        var mixer = new AudioMixer();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        mixer.SetVolume(id1, 0.5f);
        mixer.SetVolume(id2, 1.0f);

        var samples1 = new short[] { 1000 };
        var samples2 = new short[] { 1000 };

        var streams = new Dictionary<Guid, short[]>
        {
            { id1, samples1 },
            { id2, samples2 }
        };

        var result = mixer.Mix(streams, 1);

        Assert.Equal(1500, result[0]); // 500 + 1000
    }

    [Fact]
    public void SamplesToBytes_ConvertsCorrectly()
    {
        var samples = new short[] { 0x0102, unchecked((short)0xFEFF) };
        var bytes = AudioMixer.SamplesToBytes(samples);

        Assert.Equal(4, bytes.Length);
        // Little-endian: 0x0102 → 0x02, 0x01
        Assert.Equal(0x02, bytes[0]);
        Assert.Equal(0x01, bytes[1]);
    }

    [Fact]
    public void BytesToSamples_ConvertsCorrectly()
    {
        var bytes = new byte[] { 0x02, 0x01, 0xFF, 0xFE };
        var samples = AudioMixer.BytesToSamples(bytes, 0, 4);

        Assert.Equal(2, samples.Length);
        Assert.Equal(0x0102, samples[0]);
    }

    [Fact]
    public void SamplesToBytes_BytesToSamples_Roundtrip()
    {
        var original = new short[] { 100, -200, 300, -400, 32767, -32768, 0 };
        var bytes = AudioMixer.SamplesToBytes(original);
        var roundtrip = AudioMixer.BytesToSamples(bytes, 0, bytes.Length);

        Assert.Equal(original, roundtrip);
    }

    [Fact]
    public void Mix_ZeroVolume_ProducesSilence()
    {
        var mixer = new AudioMixer();
        var id = Guid.NewGuid();
        mixer.SetVolume(id, 0.0f);

        var samples = new short[] { 1000, -1000, 500 };
        var streams = new Dictionary<Guid, short[]> { { id, samples } };
        var result = mixer.Mix(streams, 3);

        Assert.All(result, s => Assert.Equal(0, s));
    }
}
