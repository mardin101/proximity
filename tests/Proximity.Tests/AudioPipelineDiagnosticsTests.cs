using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Audio.Codec;
using Proximity.Audio.Diagnostics;
using Proximity.Audio.Pipeline;
using Proximity.Core.Interfaces;

namespace Proximity.Tests;

public class AudioPipelineDiagnosticsTests
{
    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int FrameSize = 960;

    [Fact]
    public void Pipeline_WithDiagnostics_GetSnapshot_ReturnsData()
    {
        var diag = new AudioDiagnostics();
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(
            NullLogger<AudioPipeline>.Instance, codec, mixer, diagnostics: diag);

        var snap = pipeline.GetDiagnosticsSnapshot();
        Assert.NotNull(snap);
        Assert.False(snap!.IsActive);
        Assert.False(snap.IsMuted);
    }

    [Fact]
    public void Pipeline_WithoutDiagnostics_GetSnapshot_ReturnsNull()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(
            NullLogger<AudioPipeline>.Instance, codec, mixer);

        Assert.Null(pipeline.GetDiagnosticsSnapshot());
    }

    [Fact]
    public void CapturedAudio_RecordsCaptureEncodeAndSend()
    {
        var diag = new AudioDiagnostics();
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(
            NullLogger<AudioPipeline>.Instance, codec, mixer, diagnostics: diag);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();
        var localId = Guid.NewGuid();

        pipeline.Start(capture, playback, transport, localId);

        // Feed one full frame of 960 samples = 1920 bytes
        var pcm = new short[FrameSize];
        for (int i = 0; i < FrameSize; i++)
            pcm[i] = (short)(Math.Sin(2 * Math.PI * 440 * i / SampleRate) * 16000);

        var bytes = AudioMixer.SamplesToBytes(pcm);
        capture.SimulateAudioData(bytes, bytes.Length);

        var snap = pipeline.GetDiagnosticsSnapshot()!;
        Assert.Equal(1, snap.FramesCaptured);
        Assert.Equal(1, snap.FramesEncoded);
        Assert.Equal(1, snap.PacketsSent);
        Assert.Equal(0, snap.CaptureErrors);
    }

    [Fact]
    public void MutedCapture_DoesNotRecordAnyCaptureMetrics()
    {
        var diag = new AudioDiagnostics();
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(
            NullLogger<AudioPipeline>.Instance, codec, mixer, diagnostics: diag);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.IsMuted = true;
        pipeline.Start(capture, playback, transport, Guid.NewGuid());

        var bytes = new byte[FrameSize * 2];
        capture.SimulateAudioData(bytes, bytes.Length);

        var snap = pipeline.GetDiagnosticsSnapshot()!;
        Assert.Equal(0, snap.FramesCaptured);
        Assert.Equal(0, snap.FramesEncoded);
        Assert.Equal(0, snap.PacketsSent);
    }

    [Fact]
    public void ReceivedAudio_RecordsReceiveAndDecode()
    {
        var diag = new AudioDiagnostics();
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        // Use default jitterBufferMs (60ms, depth=3). Sending fewer than 3 frames
        // means the buffer is never primed and DrainJitterBuffer exits immediately.
        using var pipeline = new AudioPipeline(
            NullLogger<AudioPipeline>.Instance, codec, mixer, diagnostics: diag);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();
        var localId = Guid.NewGuid();
        var remoteId = Guid.NewGuid();

        pipeline.Start(capture, playback, transport, localId);

        // Encode a frame to get valid Opus data
        var pcm = new short[FrameSize];
        for (int i = 0; i < FrameSize; i++)
            pcm[i] = (short)(Math.Sin(2 * Math.PI * 440 * i / SampleRate) * 16000);

        var encoded = codec.Encode(pcm, FrameSize);

        // Build a packet with sequence number 0
        var packet = new byte[4 + encoded.Length];
        packet[0] = 0; packet[1] = 0; packet[2] = 0; packet[3] = 0;
        Buffer.BlockCopy(encoded, 0, packet, 4, encoded.Length);

        // Send two packets (not enough to prime the 3-frame jitter buffer)
        transport.SimulateReceive(remoteId, packet, packet.Length);

        var packet2 = new byte[4 + encoded.Length];
        packet2[0] = 0; packet2[1] = 0; packet2[2] = 0; packet2[3] = 1;
        Buffer.BlockCopy(encoded, 0, packet2, 4, encoded.Length);
        transport.SimulateReceive(remoteId, packet2, packet2.Length);

        var snap = pipeline.GetDiagnosticsSnapshot()!;
        Assert.Equal(2, snap.PacketsReceived);
        Assert.Equal(2, snap.FramesDecoded);
        Assert.Equal(0, snap.PlaybackErrors);
    }

    [Fact]
    public void Snapshot_ReflectsActiveAndMuteState()
    {
        var diag = new AudioDiagnostics();
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(
            NullLogger<AudioPipeline>.Instance, codec, mixer, diagnostics: diag);

        var snap1 = pipeline.GetDiagnosticsSnapshot()!;
        Assert.False(snap1.IsActive);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();
        pipeline.Start(capture, playback, transport, Guid.NewGuid());

        var snap2 = pipeline.GetDiagnosticsSnapshot()!;
        Assert.True(snap2.IsActive);

        pipeline.IsMuted = true;
        var snap3 = pipeline.GetDiagnosticsSnapshot()!;
        Assert.True(snap3.IsMuted);
    }

    [Fact]
    public void Snapshot_TracksActiveParticipants()
    {
        var diag = new AudioDiagnostics();
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(
            NullLogger<AudioPipeline>.Instance, codec, mixer, diagnostics: diag);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();
        var localId = Guid.NewGuid();

        pipeline.Start(capture, playback, transport, localId);

        Assert.Equal(0, pipeline.GetDiagnosticsSnapshot()!.ActiveParticipants);

        // Simulate audio from a remote participant (1 frame, buffer not primed)
        var remoteId = Guid.NewGuid();
        var pcm = new short[FrameSize];
        var encoded = codec.Encode(pcm, FrameSize);
        var packet = new byte[4 + encoded.Length];
        Buffer.BlockCopy(encoded, 0, packet, 4, encoded.Length);
        transport.SimulateReceive(remoteId, packet, packet.Length);

        Assert.Equal(1, pipeline.GetDiagnosticsSnapshot()!.ActiveParticipants);
    }

    [Fact]
    public void MultipleCaptures_AccumulateCorrectly()
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

        // Send 3 full frames
        for (int f = 0; f < 3; f++)
        {
            var pcm = new short[FrameSize];
            for (int i = 0; i < FrameSize; i++)
                pcm[i] = (short)(Math.Sin(2 * Math.PI * 440 * i / SampleRate) * 16000);
            var bytes = AudioMixer.SamplesToBytes(pcm);
            capture.SimulateAudioData(bytes, bytes.Length);
        }

        var snap = pipeline.GetDiagnosticsSnapshot()!;
        Assert.Equal(3, snap.FramesCaptured);
        Assert.Equal(3, snap.FramesEncoded);
        Assert.Equal(3, snap.PacketsSent);
    }

    private static OpusCodecWrapper CreateCodec()
    {
        return new OpusCodecWrapper(
            NullLogger<OpusCodecWrapper>.Instance,
            SampleRate, Channels, FrameSize, 32000);
    }
}
