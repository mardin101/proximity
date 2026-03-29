using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Audio.Codec;
using Proximity.Audio.Pipeline;
using Proximity.Core.Interfaces;

namespace Proximity.Tests;

public class AudioPipelineTests
{
    private const int SampleRate = 48000;
    private const int Channels = 1;
    private const int FrameSize = 960;

    [Fact]
    public void Pipeline_CanBeCreatedAndDisposed()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        Assert.False(pipeline.IsActive);
        Assert.False(pipeline.IsMuted);
    }

    [Fact]
    public void Pipeline_MuteProperty_Works()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        pipeline.IsMuted = true;
        Assert.True(pipeline.IsMuted);

        pipeline.IsMuted = false;
        Assert.False(pipeline.IsMuted);
    }

    [Fact]
    public void Pipeline_SetParticipantVolume_PropagesToMixer()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var participantId = Guid.NewGuid();
        pipeline.SetParticipantVolume(participantId, 0.5f);

        Assert.Equal(0.5f, mixer.GetVolume(participantId));
    }

    [Fact]
    public void Pipeline_RemoveParticipant_CleansUpMixer()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var participantId = Guid.NewGuid();
        pipeline.SetParticipantVolume(participantId, 0.5f);

        pipeline.RemoveParticipant(participantId);
        Assert.Equal(1.0f, mixer.GetVolume(participantId)); // Returns default after removal
    }

    [Fact]
    public void Pipeline_Start_WithMockComponents_SetsActive()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.Start(capture, playback, transport, Guid.NewGuid());

        Assert.True(pipeline.IsActive);
        Assert.True(capture.IsCapturing);
    }

    [Fact]
    public void Pipeline_Stop_DeactivatesPipeline()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.Start(capture, playback, transport, Guid.NewGuid());
        pipeline.Stop();

        Assert.False(pipeline.IsActive);
    }

    [Fact]
    public void Pipeline_Dispose_StopsPipeline()
    {
        var codec = CreateCodec();
        var mixer = new AudioMixer();
        var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.Start(capture, playback, transport, Guid.NewGuid());
        pipeline.Dispose();

        Assert.False(pipeline.IsActive);
    }

    [Fact]
    public void Pipeline_CapturedAudio_IsEncodedAndSent()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();
        var localId = Guid.NewGuid();

        pipeline.Start(capture, playback, transport, localId);

        // Simulate a full frame of audio data (960 samples = 1920 bytes at 16-bit)
        var pcm = new short[FrameSize];
        for (int i = 0; i < FrameSize; i++)
        {
            pcm[i] = (short)(Math.Sin(2 * Math.PI * 440 * i / SampleRate) * 16000);
        }
        var bytes = AudioMixer.SamplesToBytes(pcm);
        capture.SimulateAudioData(bytes, bytes.Length);

        // Verify that audio was sent via transport
        Assert.True(transport.BroadcastCount > 0, "Audio should have been broadcast via transport");
        Assert.Equal(localId, transport.LastBroadcastSenderId);
    }

    [Fact]
    public void Pipeline_MutedCapture_DoesNotSend()
    {
        using var codec = CreateCodec();
        var mixer = new AudioMixer();
        using var pipeline = new AudioPipeline(NullLogger<AudioPipeline>.Instance, codec, mixer);

        var capture = new StubAudioCapture();
        var playback = new StubAudioPlayback();
        var transport = new StubAudioTransport();

        pipeline.IsMuted = true;
        pipeline.Start(capture, playback, transport, Guid.NewGuid());

        // Simulate audio data
        var bytes = new byte[FrameSize * 2];
        capture.SimulateAudioData(bytes, bytes.Length);

        Assert.Equal(0, transport.BroadcastCount);
    }

    private static OpusCodecWrapper CreateCodec()
    {
        return new OpusCodecWrapper(
            NullLogger<OpusCodecWrapper>.Instance,
            SampleRate, Channels, FrameSize, 32000);
    }
}

/// <summary>
/// Stub audio capture for testing
/// </summary>
internal class StubAudioCapture : IAudioCapture
{
    public bool IsCapturing { get; private set; }
    public event EventHandler<AudioDataEventArgs>? AudioDataAvailable;

    public void Start() => IsCapturing = true;
    public void Stop() => IsCapturing = false;
    public void Dispose() { }

    public void SimulateAudioData(byte[] buffer, int bytesRecorded)
    {
        AudioDataAvailable?.Invoke(this, new AudioDataEventArgs(buffer, bytesRecorded));
    }
}

/// <summary>
/// Stub audio playback for testing
/// </summary>
internal class StubAudioPlayback : IAudioPlayback
{
    public List<(Guid ParticipantId, byte[] AudioData)> ReceivedSamples { get; } = new();

    public void Start() { }
    public void Stop() { }

    public void AddSamples(Guid participantId, byte[] audioData, int offset, int count)
    {
        var data = new byte[count];
        Buffer.BlockCopy(audioData, offset, data, 0, count);
        ReceivedSamples.Add((participantId, data));
    }

    public void SetParticipantVolume(Guid participantId, float volume) { }
    public void RemoveParticipant(Guid participantId) { }
    public void Dispose() { }
}

/// <summary>
/// Stub audio transport for testing
/// </summary>
internal class StubAudioTransport : IAudioTransport
{
    public int BroadcastCount { get; private set; }
    public Guid LastBroadcastSenderId { get; private set; }
    public byte[]? LastBroadcastData { get; private set; }

    public event EventHandler<AudioPacketEventArgs>? AudioReceived;

    public Task StartAsync(int port, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;

    public Task SendAudioAsync(Guid senderId, byte[] audioData, int count, string remoteAddress, int remotePort)
        => Task.CompletedTask;

    public void AddTarget(Guid participantId, string address, int port) { }
    public void RemoveTarget(Guid participantId) { }

    public Task BroadcastAudioAsync(Guid senderId, byte[] audioData, int count)
    {
        BroadcastCount++;
        LastBroadcastSenderId = senderId;
        LastBroadcastData = audioData;
        return Task.CompletedTask;
    }

    public void SimulateReceive(Guid senderId, byte[] audioData, int length)
    {
        AudioReceived?.Invoke(this, new AudioPacketEventArgs(senderId, audioData, length));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
