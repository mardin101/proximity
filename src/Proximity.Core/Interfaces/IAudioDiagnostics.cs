namespace Proximity.Core.Interfaces;

/// <summary>
/// Point-in-time snapshot of audio pipeline diagnostics
/// </summary>
public class AudioDiagnosticsSnapshot
{
    /// <summary>
    /// Number of raw PCM frames captured from the microphone
    /// </summary>
    public long FramesCaptured { get; init; }

    /// <summary>
    /// Number of frames successfully Opus-encoded
    /// </summary>
    public long FramesEncoded { get; init; }

    /// <summary>
    /// Number of encoded packets sent via transport
    /// </summary>
    public long PacketsSent { get; init; }

    /// <summary>
    /// Number of audio packets received from the network
    /// </summary>
    public long PacketsReceived { get; init; }

    /// <summary>
    /// Number of received packets successfully decoded
    /// </summary>
    public long FramesDecoded { get; init; }

    /// <summary>
    /// Number of decoded frames written to the playback device
    /// </summary>
    public long FramesPlayed { get; init; }

    /// <summary>
    /// Number of capture-side errors (encode/send failures)
    /// </summary>
    public long CaptureErrors { get; init; }

    /// <summary>
    /// Number of playback-side errors (receive/decode failures)
    /// </summary>
    public long PlaybackErrors { get; init; }

    /// <summary>
    /// Number of packets dropped by the jitter buffer (too old or duplicate)
    /// </summary>
    public long JitterBufferUnderruns { get; init; }

    /// <summary>
    /// Number of packet-loss-concealment frames generated
    /// </summary>
    public long ConcealedFrames { get; init; }

    /// <summary>
    /// Number of transport-level send failures
    /// </summary>
    public long TransportSendErrors { get; init; }

    /// <summary>
    /// Number of transport-level receive failures
    /// </summary>
    public long TransportReceiveErrors { get; init; }

    /// <summary>
    /// Current number of frames buffered across all jitter buffers
    /// </summary>
    public int CurrentJitterBufferFrames { get; init; }

    /// <summary>
    /// Number of remote participants currently tracked by the pipeline
    /// </summary>
    public int ActiveParticipants { get; init; }

    /// <summary>
    /// Timestamp when this snapshot was taken (UTC)
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Whether the pipeline is currently active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Whether the microphone is currently muted
    /// </summary>
    public bool IsMuted { get; init; }

    public override string ToString()
    {
        return $"[AudioDiag {Timestamp:HH:mm:ss.fff}] Active={IsActive} Muted={IsMuted} " +
               $"Cap={FramesCaptured} Enc={FramesEncoded} Sent={PacketsSent} " +
               $"Recv={PacketsReceived} Dec={FramesDecoded} Play={FramesPlayed} " +
               $"CapErr={CaptureErrors} PlayErr={PlaybackErrors} " +
               $"JBUnder={JitterBufferUnderruns} PLC={ConcealedFrames} " +
               $"TxErr={TransportSendErrors} RxErr={TransportReceiveErrors} " +
               $"JBFrames={CurrentJitterBufferFrames} Participants={ActiveParticipants}";
    }
}

/// <summary>
/// Interface for collecting and querying audio pipeline diagnostics.
/// Implementations must be thread-safe; counters are incremented from
/// capture, transport, and playback threads concurrently.
/// </summary>
public interface IAudioDiagnostics
{
    /// <summary>Record a captured PCM frame from the microphone</summary>
    void RecordCapture();

    /// <summary>Record a successful Opus encode</summary>
    void RecordEncode();

    /// <summary>Record a packet sent via transport</summary>
    void RecordSend();

    /// <summary>Record a packet received from the network</summary>
    void RecordReceive();

    /// <summary>Record a successful Opus decode</summary>
    void RecordDecode();

    /// <summary>Record a frame written to the playback device</summary>
    void RecordPlay();

    /// <summary>Record an error on the capture/send path</summary>
    void RecordCaptureError();

    /// <summary>Record an error on the receive/playback path</summary>
    void RecordPlaybackError();

    /// <summary>Record a jitter-buffer underrun (missing frame at playback time)</summary>
    void RecordJitterBufferUnderrun();

    /// <summary>Record a packet-loss-concealment frame</summary>
    void RecordConcealedFrame();

    /// <summary>Record a transport-level send failure</summary>
    void RecordTransportSendError();

    /// <summary>Record a transport-level receive failure</summary>
    void RecordTransportReceiveError();

    /// <summary>
    /// Return a point-in-time snapshot of all counters.
    /// <paramref name="isActive"/>, <paramref name="isMuted"/>,
    /// <paramref name="currentJitterBufferFrames"/>, and <paramref name="activeParticipants"/>
    /// are live state supplied by the caller.
    /// </summary>
    AudioDiagnosticsSnapshot GetSnapshot(
        bool isActive,
        bool isMuted,
        int currentJitterBufferFrames,
        int activeParticipants);

    /// <summary>
    /// Reset all counters to zero
    /// </summary>
    void Reset();
}
