using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Proximity.Audio.Diagnostics;
using Proximity.Core.Interfaces;

namespace Proximity.Audio.Pipeline;

/// <summary>
/// Orchestrates the full audio pipeline:
///   Capture: Microphone → WaveIn → Opus Encode → UDP Send
///   Playback: UDP Receive → Opus Decode → Jitter Buffer → Audio Mixer → WaveOut
/// Handles multiple simultaneous speakers with per-participant jitter buffering and mixing.
/// </summary>
public class AudioPipeline : IDisposable
{
    private readonly ILogger<AudioPipeline> _logger;
    private readonly IAudioCodec _codec;
    private readonly AudioMixer _mixer;
    private readonly int _frameSize;
    private readonly int _jitterBufferMs;
    private readonly IAudioDiagnostics? _diagnostics;

    private IAudioCapture? _capture;
    private IAudioPlayback? _playback;
    private IAudioTransport? _transport;
    private Guid _localParticipantId;

    // Per-participant state for decoding and jitter buffering
    private readonly ConcurrentDictionary<Guid, ParticipantAudioState> _participantStates = new();

    // Accumulation buffer for capturing (WaveIn may send chunks that don't align to frame boundaries)
    private short[] _captureAccumulator = Array.Empty<short>();
    private int _captureAccumulatorOffset;

    private uint _sendSequence;
    private bool _isMuted;
    private bool _disposed;

    /// <summary>
    /// Whether the pipeline is currently active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Whether the local user's microphone is muted
    /// </summary>
    public bool IsMuted
    {
        get => _isMuted;
        set => _isMuted = value;
    }

    public AudioPipeline(ILogger<AudioPipeline> logger, IAudioCodec codec, AudioMixer mixer, int jitterBufferMs = 60, IAudioDiagnostics? diagnostics = null)
    {
        _logger = logger;
        _codec = codec;
        _mixer = mixer;
        _frameSize = codec.FrameSize;
        _jitterBufferMs = jitterBufferMs;
        _diagnostics = diagnostics;
        _captureAccumulator = new short[_frameSize * 2]; // Double buffer
    }

    /// <summary>
    /// Get a point-in-time diagnostics snapshot of the audio pipeline.
    /// Returns null when no diagnostics instance was provided.
    /// </summary>
    public AudioDiagnosticsSnapshot? GetDiagnosticsSnapshot()
    {
        if (_diagnostics == null) return null;

        int jitterFrames = 0;
        foreach (var state in _participantStates.Values)
        {
            jitterFrames += state.JitterBuffer.BufferedFrames;
        }

        return _diagnostics.GetSnapshot(IsActive, IsMuted, jitterFrames, _participantStates.Count);
    }

    /// <summary>
    /// Start the audio pipeline with the given capture, playback, and transport components
    /// </summary>
    public void Start(IAudioCapture capture, IAudioPlayback playback, IAudioTransport transport, Guid localParticipantId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _capture = capture;
        _playback = playback;
        _transport = transport;
        _localParticipantId = localParticipantId;
        _sendSequence = 0;
        _captureAccumulatorOffset = 0;

        // Wire up capture events
        _capture.AudioDataAvailable += OnAudioCaptured;

        // Wire up transport receive events
        _transport.AudioReceived += OnAudioReceived;

        // Start components
        _playback.Start();
        _capture.Start();

        IsActive = true;
        _logger.LogInformation("Audio pipeline started for participant {ParticipantId}", localParticipantId);
    }

    /// <summary>
    /// Stop the audio pipeline
    /// </summary>
    public void Stop()
    {
        if (!IsActive) return;

        if (_capture != null)
        {
            _capture.AudioDataAvailable -= OnAudioCaptured;
            _capture.Stop();
        }

        if (_transport != null)
        {
            _transport.AudioReceived -= OnAudioReceived;
        }

        _playback?.Stop();

        // Clean up participant states
        foreach (var state in _participantStates.Values)
        {
            state.Codec.Dispose();
        }
        _participantStates.Clear();

        IsActive = false;
        _logger.LogInformation("Audio pipeline stopped");
    }

    /// <summary>
    /// Set the volume for a specific participant
    /// </summary>
    public void SetParticipantVolume(Guid participantId, float volume)
    {
        _mixer.SetVolume(participantId, volume);
        _playback?.SetParticipantVolume(participantId, volume);
    }

    /// <summary>
    /// Remove a participant from the pipeline
    /// </summary>
    public void RemoveParticipant(Guid participantId)
    {
        if (_participantStates.TryRemove(participantId, out var state))
        {
            state.Codec.Dispose();
        }
        _mixer.RemoveParticipant(participantId);
        _playback?.RemoveParticipant(participantId);
    }

    /// <summary>
    /// Handle captured audio from the microphone.
    /// Accumulates samples into frame-sized chunks, encodes with Opus, and sends via transport.
    /// </summary>
    private void OnAudioCaptured(object? sender, AudioDataEventArgs e)
    {
        if (_isMuted || _transport == null) return;

        try
        {
            // Convert bytes to PCM samples
            var samples = AudioMixer.BytesToSamples(e.Buffer, 0, e.BytesRecorded);
            _diagnostics?.RecordCapture();

            // Accumulate samples into frame-sized chunks
            int samplesRemaining = samples.Length;
            int sourceOffset = 0;

            while (samplesRemaining > 0)
            {
                int spaceInAccumulator = _frameSize - _captureAccumulatorOffset;
                int toCopy = Math.Min(samplesRemaining, spaceInAccumulator);

                Array.Copy(samples, sourceOffset, _captureAccumulator, _captureAccumulatorOffset, toCopy);
                _captureAccumulatorOffset += toCopy;
                sourceOffset += toCopy;
                samplesRemaining -= toCopy;

                // When we have a full frame, encode and send
                if (_captureAccumulatorOffset >= _frameSize)
                {
                    var frameToEncode = new short[_frameSize];
                    Array.Copy(_captureAccumulator, 0, frameToEncode, 0, _frameSize);

                    var encoded = _codec.Encode(frameToEncode, _frameSize);
                    _diagnostics?.RecordEncode();

                    var packet = BuildPacket(_sendSequence++, encoded);

                    _ = _transport.BroadcastAudioAsync(_localParticipantId, packet, packet.Length);
                    _diagnostics?.RecordSend();

                    _captureAccumulatorOffset = 0;
                }
            }
        }
        catch (Exception ex)
        {
            _diagnostics?.RecordCaptureError();
            _logger.LogDebug(ex, "Error processing captured audio");
        }
    }

    /// <summary>
    /// Handle received audio from the transport.
    /// Decodes the Opus packet, buffers it in the jitter buffer, and sends to playback.
    /// </summary>
    private void OnAudioReceived(object? sender, AudioPacketEventArgs e)
    {
        if (_playback == null) return;

        // Don't play back our own audio
        if (e.SenderId == _localParticipantId) return;

        try
        {
            _diagnostics?.RecordReceive();

            var (sequenceNumber, opusData) = ParsePacket(e.AudioData, e.Length);

            // Get or create per-participant state
            var state = _participantStates.GetOrAdd(e.SenderId, id =>
            {
                _logger.LogDebug("Creating audio state for participant {ParticipantId}", id);
                int frameSizeMs = _codec.FrameSize * 1000 / _codec.SampleRate;
                return new ParticipantAudioState(
                    new Codec.OpusCodecWrapper(
                        Microsoft.Extensions.Logging.Abstractions.NullLogger<Codec.OpusCodecWrapper>.Instance,
                        _codec.SampleRate, _codec.Channels, _codec.FrameSize),
                    new JitterBuffer(_logger, _jitterBufferMs, frameSizeMs));
            });

            // Decode and add to jitter buffer
            var decoded = state.Codec.Decode(opusData, opusData.Length);
            _diagnostics?.RecordDecode();

            state.JitterBuffer.AddPacket(sequenceNumber, decoded);

            // Drain jitter buffer frames into playback
            DrainJitterBuffer(e.SenderId, state);
        }
        catch (Exception ex)
        {
            _diagnostics?.RecordPlaybackError();
            _logger.LogDebug(ex, "Error processing received audio from {SenderId}", e.SenderId);
        }
    }

    private void DrainJitterBuffer(Guid participantId, ParticipantAudioState state)
    {
        while (true)
        {
            var frame = state.JitterBuffer.GetNextFrame(out bool isMissing);

            if (frame == null && !isMissing)
            {
                break; // Buffer not primed yet
            }

            short[] pcmSamples;
            if (frame != null)
            {
                pcmSamples = frame;
            }
            else
            {
                // Packet loss concealment
                _diagnostics?.RecordJitterBufferUnderrun();
                pcmSamples = state.Codec.DecodePLC();
                _diagnostics?.RecordConcealedFrame();
            }

            // Apply volume and send to playback
            var streams = new Dictionary<Guid, short[]> { { participantId, pcmSamples } };
            var mixed = _mixer.Mix(streams, _frameSize);
            var bytes = AudioMixer.SamplesToBytes(mixed);

            _playback?.AddSamples(participantId, bytes, 0, bytes.Length);
            _diagnostics?.RecordPlay();
        }
    }

    /// <summary>
    /// Build an audio packet with sequence number header.
    /// Packet format: [4 bytes sequence (big-endian)][opus data...]
    /// </summary>
    private static byte[] BuildPacket(uint sequenceNumber, byte[] opusData)
    {
        var packet = new byte[4 + opusData.Length];
        packet[0] = (byte)(sequenceNumber >> 24);
        packet[1] = (byte)(sequenceNumber >> 16);
        packet[2] = (byte)(sequenceNumber >> 8);
        packet[3] = (byte)(sequenceNumber);
        Buffer.BlockCopy(opusData, 0, packet, 4, opusData.Length);
        return packet;
    }

    /// <summary>
    /// Parse a received audio packet to extract sequence number and Opus data.
    /// </summary>
    private static (uint sequenceNumber, byte[] opusData) ParsePacket(byte[] data, int length)
    {
        uint sequenceNumber = (uint)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
        var opusData = new byte[length - 4];
        Buffer.BlockCopy(data, 4, opusData, 0, length - 4);
        return (sequenceNumber, opusData);
    }

    /// <summary>
    /// Play a generated sine-wave test tone directly through the output device.
    /// Bypasses the network and jitter buffer so you can verify the playback
    /// device works independently of everything else.
    /// </summary>
    /// <param name="frequencyHz">Tone frequency in Hz (default 440 = A4)</param>
    /// <param name="durationMs">Duration in milliseconds (default 1000)</param>
    /// <param name="amplitude">Amplitude 0.0–1.0 (default 0.5)</param>
    public void PlayTestTone(int frequencyHz = 440, int durationMs = 1000, double amplitude = 0.5)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_playback == null) throw new InvalidOperationException("Pipeline must be started before playing a test tone.");

        _logger.LogInformation("Playing test tone: {Freq}Hz, {Duration}ms, amplitude={Amp:F2}", frequencyHz, durationMs, amplitude);

        var pcm = ToneGenerator.GenerateSineTone(frequencyHz, durationMs, _codec.SampleRate, amplitude);
        var bytes = AudioMixer.SamplesToBytes(pcm);

        // Feed into playback using a synthetic participant ID so it doesn't
        // interfere with real participants.
        var toneParticipantId = Guid.Empty;
        _playback.AddSamples(toneParticipantId, bytes, 0, bytes.Length);

        _logger.LogInformation("Test tone enqueued: {Samples} samples ({Bytes} bytes)", pcm.Length, bytes.Length);
    }

    /// <summary>
    /// Run a loopback test that encodes a test tone through Opus and decodes
    /// it back, then plays the result through the output device.
    /// This exercises the full codec path (encode → decode) without
    /// requiring the network, helping isolate codec or output device issues.
    /// </summary>
    /// <param name="frequencyHz">Tone frequency in Hz (default 440 = A4)</param>
    /// <param name="durationMs">Duration in milliseconds (default 1000)</param>
    /// <param name="amplitude">Amplitude 0.0–1.0 (default 0.5)</param>
    /// <returns>Result summary describing what happened during the loopback test.</returns>
    public LoopbackTestResult RunLoopbackTest(int frequencyHz = 440, int durationMs = 1000, double amplitude = 0.5)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_playback == null) throw new InvalidOperationException("Pipeline must be started before running a loopback test.");

        _logger.LogInformation("Running loopback test: {Freq}Hz, {Duration}ms, amplitude={Amp:F2}", frequencyHz, durationMs, amplitude);

        var pcm = ToneGenerator.GenerateSineTone(frequencyHz, durationMs, _codec.SampleRate, amplitude);
        var frames = ToneGenerator.SplitIntoFrames(pcm, _frameSize);

        int framesEncoded = 0;
        int framesDecoded = 0;
        int totalSamplesPlayed = 0;
        var errors = new List<string>();
        var toneParticipantId = Guid.Empty;

        foreach (var frame in frames)
        {
            try
            {
                var encoded = _codec.Encode(frame, _frameSize);
                framesEncoded++;

                var decoded = _codec.Decode(encoded, encoded.Length);
                framesDecoded++;

                var bytes = AudioMixer.SamplesToBytes(decoded);
                _playback.AddSamples(toneParticipantId, bytes, 0, bytes.Length);
                totalSamplesPlayed += decoded.Length;
            }
            catch (Exception ex)
            {
                var msg = $"Frame {framesEncoded}: {ex.Message}";
                errors.Add(msg);
                _logger.LogWarning(ex, "Loopback test error at frame {Frame}", framesEncoded);
            }
        }

        var result = new LoopbackTestResult(
            TotalFrames: frames.Count,
            FramesEncoded: framesEncoded,
            FramesDecoded: framesDecoded,
            TotalSamplesPlayed: totalSamplesPlayed,
            Errors: errors);

        _logger.LogInformation(
            "Loopback test complete: {Encoded}/{Total} encoded, {Decoded}/{Total} decoded, {Samples} samples played, {Errors} errors",
            result.FramesEncoded, result.TotalFrames,
            result.FramesDecoded, result.TotalFrames,
            result.TotalSamplesPlayed, result.Errors.Count);

        return result;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Stop();
            _logger.LogDebug("Audio pipeline disposed");
        }
    }

    /// <summary>
    /// Per-participant audio processing state
    /// </summary>
    private sealed class ParticipantAudioState
    {
        public IAudioCodec Codec { get; }
        public JitterBuffer JitterBuffer { get; }

        public ParticipantAudioState(IAudioCodec codec, JitterBuffer jitterBuffer)
        {
            Codec = codec;
            JitterBuffer = jitterBuffer;
        }
    }
}

/// <summary>
/// Result of a loopback test run through <see cref="AudioPipeline.RunLoopbackTest"/>.
/// </summary>
public record LoopbackTestResult(
    int TotalFrames,
    int FramesEncoded,
    int FramesDecoded,
    int TotalSamplesPlayed,
    IReadOnlyList<string> Errors)
{
    /// <summary>Whether the test completed without any errors.</summary>
    public bool Success => Errors.Count == 0 && FramesEncoded == TotalFrames && FramesDecoded == TotalFrames;
}
