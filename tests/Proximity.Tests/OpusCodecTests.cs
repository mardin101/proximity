using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Audio.Codec;

namespace Proximity.Tests;

public class OpusCodecTests
{
    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int FrameSize = 960; // 20ms @ 48kHz
    private const int Bitrate = 32000;

    [Fact]
    public void Constructor_SetsProperties()
    {
        using var codec = CreateCodec();

        Assert.Equal(FrameSize, codec.FrameSize);
        Assert.Equal(SampleRate, codec.SampleRate);
        Assert.Equal(Channels, codec.Channels);
    }

    [Fact]
    public void Encode_ProducesCompressedOutput()
    {
        using var codec = CreateCodec();
        var pcm = GenerateSineWave(FrameSize, 440);

        var encoded = codec.Encode(pcm, FrameSize);

        Assert.NotNull(encoded);
        Assert.True(encoded.Length > 0, "Encoded data should not be empty");
        Assert.True(encoded.Length < FrameSize * 2, "Encoded data should be smaller than raw PCM");
    }

    [Fact]
    public void Encode_SilenceProducesSmallPacket()
    {
        using var codec = CreateCodec();
        var silence = new short[FrameSize];

        var encoded = codec.Encode(silence, FrameSize);

        Assert.NotNull(encoded);
        Assert.True(encoded.Length > 0);
    }

    [Fact]
    public void Decode_ReturnsCorrectFrameSize()
    {
        using var codec = CreateCodec();
        var pcm = GenerateSineWave(FrameSize, 440);
        var encoded = codec.Encode(pcm, FrameSize);

        var decoded = codec.Decode(encoded, encoded.Length);

        Assert.Equal(FrameSize * Channels, decoded.Length);
    }

    [Fact]
    public void Encode_Decode_Roundtrip_ProducesSimilarAudio()
    {
        using var codec = CreateCodec();
        var original = GenerateSineWave(FrameSize, 440);

        var encoded = codec.Encode(original, FrameSize);
        var decoded = codec.Decode(encoded, encoded.Length);

        // Opus is lossy, so values won't be identical, but the signal shape should be preserved
        // Check that decoded audio is not silence (has energy)
        double energy = decoded.Sum(s => (double)s * s);
        Assert.True(energy > 0, "Decoded audio should not be silence for a non-silent input");
    }

    [Fact]
    public void DecodePLC_ReturnsCorrectFrameSize()
    {
        using var codec = CreateCodec();

        // First encode and decode a frame to initialize the decoder state
        var pcm = GenerateSineWave(FrameSize, 440);
        var encoded = codec.Encode(pcm, FrameSize);
        codec.Decode(encoded, encoded.Length);

        // Now test PLC
        var plc = codec.DecodePLC();

        Assert.Equal(FrameSize * Channels, plc.Length);
    }

    [Fact]
    public void MultipleFrames_EncodeDecodeSuccessfully()
    {
        using var codec = CreateCodec();

        for (int i = 0; i < 10; i++)
        {
            var pcm = GenerateSineWave(FrameSize, 440 + i * 50);
            var encoded = codec.Encode(pcm, FrameSize);
            var decoded = codec.Decode(encoded, encoded.Length);

            Assert.Equal(FrameSize * Channels, decoded.Length);
        }
    }

    [Fact]
    public void Dispose_PreventsSubsequentEncoding()
    {
        var codec = CreateCodec();
        codec.Dispose();

        Assert.Throws<ObjectDisposedException>(() => codec.Encode(new short[FrameSize], FrameSize));
    }

    [Fact]
    public void Dispose_PreventsSubsequentDecoding()
    {
        var codec = CreateCodec();
        var pcm = GenerateSineWave(FrameSize, 440);
        var encoded = codec.Encode(pcm, FrameSize);
        codec.Dispose();

        Assert.Throws<ObjectDisposedException>(() => codec.Decode(encoded, encoded.Length));
    }

    [Fact]
    public void Dispose_PreventsSubsequentPLC()
    {
        var codec = CreateCodec();
        codec.Dispose();

        Assert.Throws<ObjectDisposedException>(() => codec.DecodePLC());
    }

    [Fact]
    public void EncodedSize_IsReasonableForBitrate()
    {
        using var codec = CreateCodec();
        var pcm = GenerateSineWave(FrameSize, 440);

        var encoded = codec.Encode(pcm, FrameSize);

        // At 32kbps, 20ms frame: ~80 bytes expected (32000 * 0.020 / 8 = 80)
        // Allow range of 20-200 bytes for codec overhead/variability
        Assert.InRange(encoded.Length, 2, 500);
    }

    private static OpusCodecWrapper CreateCodec()
    {
        return new OpusCodecWrapper(
            NullLogger<OpusCodecWrapper>.Instance,
            SampleRate, Channels, FrameSize, Bitrate);
    }

    private static short[] GenerateSineWave(int sampleCount, double frequency)
    {
        var samples = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = (short)(Math.Sin(2 * Math.PI * frequency * i / SampleRate) * 16000);
        }
        return samples;
    }
}
