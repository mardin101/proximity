using System.Threading;
using Proximity.Core.Interfaces;

namespace Proximity.Audio.Diagnostics;

/// <summary>
/// Thread-safe implementation of <see cref="IAudioDiagnostics"/> backed by
/// <see cref="Interlocked"/> counters so that capture, transport, and
/// playback threads can record metrics without contention.
/// </summary>
public class AudioDiagnostics : IAudioDiagnostics
{
    private long _framesCaptured;
    private long _framesEncoded;
    private long _packetsSent;
    private long _packetsReceived;
    private long _framesDecoded;
    private long _framesPlayed;
    private long _captureErrors;
    private long _playbackErrors;
    private long _jitterBufferUnderruns;
    private long _concealedFrames;
    private long _transportSendErrors;
    private long _transportReceiveErrors;

    /// <inheritdoc />
    public void RecordCapture() => Interlocked.Increment(ref _framesCaptured);

    /// <inheritdoc />
    public void RecordEncode() => Interlocked.Increment(ref _framesEncoded);

    /// <inheritdoc />
    public void RecordSend() => Interlocked.Increment(ref _packetsSent);

    /// <inheritdoc />
    public void RecordReceive() => Interlocked.Increment(ref _packetsReceived);

    /// <inheritdoc />
    public void RecordDecode() => Interlocked.Increment(ref _framesDecoded);

    /// <inheritdoc />
    public void RecordPlay() => Interlocked.Increment(ref _framesPlayed);

    /// <inheritdoc />
    public void RecordCaptureError() => Interlocked.Increment(ref _captureErrors);

    /// <inheritdoc />
    public void RecordPlaybackError() => Interlocked.Increment(ref _playbackErrors);

    /// <inheritdoc />
    public void RecordJitterBufferUnderrun() => Interlocked.Increment(ref _jitterBufferUnderruns);

    /// <inheritdoc />
    public void RecordConcealedFrame() => Interlocked.Increment(ref _concealedFrames);

    /// <inheritdoc />
    public void RecordTransportSendError() => Interlocked.Increment(ref _transportSendErrors);

    /// <inheritdoc />
    public void RecordTransportReceiveError() => Interlocked.Increment(ref _transportReceiveErrors);

    /// <inheritdoc />
    public AudioDiagnosticsSnapshot GetSnapshot(
        bool isActive,
        bool isMuted,
        int currentJitterBufferFrames,
        int activeParticipants)
    {
        return new AudioDiagnosticsSnapshot
        {
            FramesCaptured = Interlocked.Read(ref _framesCaptured),
            FramesEncoded = Interlocked.Read(ref _framesEncoded),
            PacketsSent = Interlocked.Read(ref _packetsSent),
            PacketsReceived = Interlocked.Read(ref _packetsReceived),
            FramesDecoded = Interlocked.Read(ref _framesDecoded),
            FramesPlayed = Interlocked.Read(ref _framesPlayed),
            CaptureErrors = Interlocked.Read(ref _captureErrors),
            PlaybackErrors = Interlocked.Read(ref _playbackErrors),
            JitterBufferUnderruns = Interlocked.Read(ref _jitterBufferUnderruns),
            ConcealedFrames = Interlocked.Read(ref _concealedFrames),
            TransportSendErrors = Interlocked.Read(ref _transportSendErrors),
            TransportReceiveErrors = Interlocked.Read(ref _transportReceiveErrors),
            CurrentJitterBufferFrames = currentJitterBufferFrames,
            ActiveParticipants = activeParticipants,
            Timestamp = DateTimeOffset.UtcNow,
            IsActive = isActive,
            IsMuted = isMuted
        };
    }

    /// <inheritdoc />
    public void Reset()
    {
        Interlocked.Exchange(ref _framesCaptured, 0);
        Interlocked.Exchange(ref _framesEncoded, 0);
        Interlocked.Exchange(ref _packetsSent, 0);
        Interlocked.Exchange(ref _packetsReceived, 0);
        Interlocked.Exchange(ref _framesDecoded, 0);
        Interlocked.Exchange(ref _framesPlayed, 0);
        Interlocked.Exchange(ref _captureErrors, 0);
        Interlocked.Exchange(ref _playbackErrors, 0);
        Interlocked.Exchange(ref _jitterBufferUnderruns, 0);
        Interlocked.Exchange(ref _concealedFrames, 0);
        Interlocked.Exchange(ref _transportSendErrors, 0);
        Interlocked.Exchange(ref _transportReceiveErrors, 0);
    }
}
