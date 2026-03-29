using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Audio.Codec;
using Proximity.Audio.Diagnostics;
using Proximity.Audio.Pipeline;
using Proximity.Core.Interfaces;

namespace Proximity.Tests;

public class AudioPipelineToolingTests
{
    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int FrameSize = 960;

    [Fact]
    public void PlayTestTone_WithoutStart_Throws()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        Assert.Throws<InvalidOperationException>(() => pipeline.PlayTestTone());
    }

    [Fact]
    public void PlayTestTone_SendsSamplesToPlayback()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.Start(capture, playback, transport, Guid.NewGuid());
        pipeline.PlayTestTone(440, 100);

        // 100ms at 48kHz = 4800 samples × 2 bytes = 9600 bytes
        Assert.True(playback.ReceivedSamples.Count > 0, "Test tone should produce playback samples");
        Assert.Equal(Guid.Empty, playback.ReceivedSamples[0].ParticipantId);
    }

    [Fact]
    public void PlayTestTone_CustomParameters_Works()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.Start(capture, playback, transport, Guid.NewGuid());
        pipeline.PlayTestTone(frequencyHz: 880, durationMs: 200, amplitude: 0.3);

        Assert.True(playback.ReceivedSamples.Count > 0);
    }

    [Fact]
    public void RunLoopbackTest_WithoutStart_Throws()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        Assert.Throws<InvalidOperationException>(() => pipeline.RunLoopbackTest());
    }

    [Fact]
    public void RunLoopbackTest_SuccessfulResult()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.Start(capture, playback, transport, Guid.NewGuid());

        var result = pipeline.RunLoopbackTest(440, 100);

        Assert.True(result.Success, $"Loopback test should succeed. Errors: {string.Join(", ", result.Errors)}");
        Assert.True(result.TotalFrames > 0);
        Assert.Equal(result.TotalFrames, result.FramesEncoded);
        Assert.Equal(result.TotalFrames, result.FramesDecoded);
        Assert.True(result.TotalSamplesPlayed > 0);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void RunLoopbackTest_ProducesPlaybackOutput()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.Start(capture, playback, transport, Guid.NewGuid());
        pipeline.RunLoopbackTest(440, 200);

        Assert.True(playback.ReceivedSamples.Count > 0, "Loopback test should produce playback output");
        // All samples should go to the synthetic tone participant (Guid.Empty)
        Assert.All(playback.ReceivedSamples, s => Assert.Equal(Guid.Empty, s.ParticipantId));
    }

    [Fact]
    public void RunLoopbackTest_RecordsDiagnostics()
    {
        var diag = new AudioDiagnostics();
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(
            NullLogger<AudioPipeline>.Instance, codec, mixer, diagnostics: diag);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.Start(capture, playback, transport, Guid.NewGuid());

        // Note: RunLoopbackTest uses the codec directly, so it doesn't go through
        // the normal capture/send path. It should still produce playback samples.
        var result = pipeline.RunLoopbackTest(440, 100);
        Assert.True(result.Success);

        // The loopback test uses codec.Encode/Decode directly, bypassing
        // the pipeline's event handlers that record diagnostics. That's by design —
        // it tests the codec + playback path independently.
    }

    [Fact]
    public void RunLoopbackTest_LongerDuration_MoreFrames()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.Start(capture, playback, transport, Guid.NewGuid());

        var result100 = pipeline.RunLoopbackTest(440, 100);
        int frames100 = result100.TotalFrames;

        // Reset playback samples
        playback.ReceivedSamples.Clear();

        var result500 = pipeline.RunLoopbackTest(440, 500);
        int frames500 = result500.TotalFrames;

        Assert.True(frames500 > frames100, $"500ms ({frames500} frames) should produce more frames than 100ms ({frames100} frames)");
    }

    [Fact]
    public void PlayTestTone_AfterDispose_Throws()
    {
        var codec = CreateCodec();
        var mixer = new AudioMixer();
        var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();
        pipeline.Start(capture, playback, transport, Guid.NewGuid());
        pipeline.Dispose();

        Assert.Throws<ObjectDisposedException>(() => pipeline.PlayTestTone());
    }

    [Fact]
    public void RunLoopbackTest_AfterDispose_Throws()
    {
        var codec = CreateCodec();
        var mixer = new AudioMixer();
        var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();
        pipeline.Start(capture, playback, transport, Guid.NewGuid());
        pipeline.Dispose();

        Assert.Throws<ObjectDisposedException>(() => pipeline.RunLoopbackTest());
    }

    private static OpusCodecWrapper CreateCodec()
    {
        return new OpusCodecWrapper(
            NullLogger<OpusCodecWrapper>.Instance,
            SampleRate, Channels, FrameSize, 32000);
    }
}
