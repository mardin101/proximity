using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;

namespace Proximity.Audio.Diagnostics;

/// <summary>
/// Periodically logs <see cref="AudioDiagnosticsSnapshot"/> summaries so that the
/// audio pipeline state is continuously visible in logs without manual polling.
/// </summary>
public sealed class DiagnosticsLogger : IDisposable
{
    private readonly ILogger _logger;
    private readonly Func<AudioDiagnosticsSnapshot?> _snapshotProvider;
    private readonly Timer _timer;
    private AudioDiagnosticsSnapshot? _previousSnapshot;
    private bool _disposed;

    /// <summary>
    /// Interval between log entries.
    /// </summary>
    public TimeSpan Interval { get; }

    /// <summary>
    /// Create a diagnostics logger that periodically polls a snapshot and logs it.
    /// </summary>
    /// <param name="logger">Logger to write to</param>
    /// <param name="snapshotProvider">
    /// Delegate that returns the current snapshot (e.g., <c>() => pipeline.GetDiagnosticsSnapshot()</c>).
    /// May return <c>null</c> if diagnostics are not available.
    /// </param>
    /// <param name="intervalMs">Interval in milliseconds between log entries (default 5000)</param>
    public DiagnosticsLogger(ILogger logger, Func<AudioDiagnosticsSnapshot?> snapshotProvider, int intervalMs = 5000)
    {
        if (intervalMs <= 0) throw new ArgumentOutOfRangeException(nameof(intervalMs), "Interval must be positive.");

        _logger = logger;
        _snapshotProvider = snapshotProvider;
        Interval = TimeSpan.FromMilliseconds(intervalMs);

        // Timer starts immediately; the first tick fires after one interval.
        _timer = new Timer(OnTick, null, Interval, Interval);
    }

    private void OnTick(object? state)
    {
        if (_disposed) return;

        try
        {
            var snapshot = _snapshotProvider();
            if (snapshot is null) return;

            var prev = _previousSnapshot;
            _previousSnapshot = snapshot;

            if (prev is null)
            {
                _logger.LogInformation("[AudioDiag] {Snapshot}", snapshot);
                return;
            }

            // Compute deltas so operators can see per-interval throughput
            long dCap = snapshot.FramesCaptured - prev.FramesCaptured;
            long dEnc = snapshot.FramesEncoded - prev.FramesEncoded;
            long dSent = snapshot.PacketsSent - prev.PacketsSent;
            long dRecv = snapshot.PacketsReceived - prev.PacketsReceived;
            long dDec = snapshot.FramesDecoded - prev.FramesDecoded;
            long dPlay = snapshot.FramesPlayed - prev.FramesPlayed;
            long dCapErr = snapshot.CaptureErrors - prev.CaptureErrors;
            long dPlayErr = snapshot.PlaybackErrors - prev.PlaybackErrors;
            long dJBUnder = snapshot.JitterBufferUnderruns - prev.JitterBufferUnderruns;
            long dPLC = snapshot.ConcealedFrames - prev.ConcealedFrames;

            _logger.LogInformation(
                "[AudioDiag] Active={Active} Muted={Muted} " +
                "Δ Cap={DCap} Enc={DEnc} Sent={DSent} Recv={DRecv} Dec={DDec} Play={DPlay} " +
                "CapErr={DCapErr} PlayErr={DPlayErr} JBUnder={DJBUnder} PLC={DPLC} " +
                "| JBFrames={JBFrames} Participants={Participants}",
                snapshot.IsActive, snapshot.IsMuted,
                dCap, dEnc, dSent, dRecv, dDec, dPlay,
                dCapErr, dPlayErr, dJBUnder, dPLC,
                snapshot.CurrentJitterBufferFrames, snapshot.ActiveParticipants);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error logging audio diagnostics");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _timer.Dispose();
        }
    }
}
