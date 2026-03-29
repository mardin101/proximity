using Proximity.Audio.Diagnostics;
using Proximity.Core.Interfaces;

namespace Proximity.Tests;

public class AudioDiagnosticsTests
{
    [Fact]
    public void NewInstance_HasAllZeroCounters()
    {
        var diag = new AudioDiagnostics();
        var snap = diag.GetSnapshot(isActive: false, isMuted: false,
            currentJitterBufferFrames: 0, activeParticipants: 0);

        Assert.Equal(0, snap.FramesCaptured);
        Assert.Equal(0, snap.FramesEncoded);
        Assert.Equal(0, snap.PacketsSent);
        Assert.Equal(0, snap.PacketsReceived);
        Assert.Equal(0, snap.FramesDecoded);
        Assert.Equal(0, snap.FramesPlayed);
        Assert.Equal(0, snap.CaptureErrors);
        Assert.Equal(0, snap.PlaybackErrors);
        Assert.Equal(0, snap.JitterBufferUnderruns);
        Assert.Equal(0, snap.ConcealedFrames);
        Assert.Equal(0, snap.TransportSendErrors);
        Assert.Equal(0, snap.TransportReceiveErrors);
    }

    [Fact]
    public void RecordCapture_IncrementsFramesCaptured()
    {
        var diag = new AudioDiagnostics();
        diag.RecordCapture();
        diag.RecordCapture();
        diag.RecordCapture();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(3, snap.FramesCaptured);
    }

    [Fact]
    public void RecordEncode_IncrementsFramesEncoded()
    {
        var diag = new AudioDiagnostics();
        diag.RecordEncode();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(1, snap.FramesEncoded);
    }

    [Fact]
    public void RecordSend_IncrementsPacketsSent()
    {
        var diag = new AudioDiagnostics();
        diag.RecordSend();
        diag.RecordSend();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(2, snap.PacketsSent);
    }

    [Fact]
    public void RecordReceive_IncrementsPacketsReceived()
    {
        var diag = new AudioDiagnostics();
        diag.RecordReceive();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(1, snap.PacketsReceived);
    }

    [Fact]
    public void RecordDecode_IncrementsFramesDecoded()
    {
        var diag = new AudioDiagnostics();
        diag.RecordDecode();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(1, snap.FramesDecoded);
    }

    [Fact]
    public void RecordPlay_IncrementsFramesPlayed()
    {
        var diag = new AudioDiagnostics();
        diag.RecordPlay();
        diag.RecordPlay();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(2, snap.FramesPlayed);
    }

    [Fact]
    public void RecordCaptureError_IncrementsCaptureErrors()
    {
        var diag = new AudioDiagnostics();
        diag.RecordCaptureError();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(1, snap.CaptureErrors);
    }

    [Fact]
    public void RecordPlaybackError_IncrementsPlaybackErrors()
    {
        var diag = new AudioDiagnostics();
        diag.RecordPlaybackError();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(1, snap.PlaybackErrors);
    }

    [Fact]
    public void RecordJitterBufferUnderrun_IncrementsUnderruns()
    {
        var diag = new AudioDiagnostics();
        diag.RecordJitterBufferUnderrun();
        diag.RecordJitterBufferUnderrun();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(2, snap.JitterBufferUnderruns);
    }

    [Fact]
    public void RecordConcealedFrame_IncrementsConcealedFrames()
    {
        var diag = new AudioDiagnostics();
        diag.RecordConcealedFrame();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(1, snap.ConcealedFrames);
    }

    [Fact]
    public void RecordTransportSendError_IncrementsSendErrors()
    {
        var diag = new AudioDiagnostics();
        diag.RecordTransportSendError();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(1, snap.TransportSendErrors);
    }

    [Fact]
    public void RecordTransportReceiveError_IncrementsReceiveErrors()
    {
        var diag = new AudioDiagnostics();
        diag.RecordTransportReceiveError();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(1, snap.TransportReceiveErrors);
    }

    [Fact]
    public void GetSnapshot_ReflectsLiveState()
    {
        var diag = new AudioDiagnostics();

        var snap = diag.GetSnapshot(isActive: true, isMuted: true,
            currentJitterBufferFrames: 5, activeParticipants: 2);

        Assert.True(snap.IsActive);
        Assert.True(snap.IsMuted);
        Assert.Equal(5, snap.CurrentJitterBufferFrames);
        Assert.Equal(2, snap.ActiveParticipants);
        Assert.True(snap.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Reset_ClearsAllCounters()
    {
        var diag = new AudioDiagnostics();
        diag.RecordCapture();
        diag.RecordEncode();
        diag.RecordSend();
        diag.RecordReceive();
        diag.RecordDecode();
        diag.RecordPlay();
        diag.RecordCaptureError();
        diag.RecordPlaybackError();
        diag.RecordJitterBufferUnderrun();
        diag.RecordConcealedFrame();
        diag.RecordTransportSendError();
        diag.RecordTransportReceiveError();

        diag.Reset();

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(0, snap.FramesCaptured);
        Assert.Equal(0, snap.FramesEncoded);
        Assert.Equal(0, snap.PacketsSent);
        Assert.Equal(0, snap.PacketsReceived);
        Assert.Equal(0, snap.FramesDecoded);
        Assert.Equal(0, snap.FramesPlayed);
        Assert.Equal(0, snap.CaptureErrors);
        Assert.Equal(0, snap.PlaybackErrors);
        Assert.Equal(0, snap.JitterBufferUnderruns);
        Assert.Equal(0, snap.ConcealedFrames);
        Assert.Equal(0, snap.TransportSendErrors);
        Assert.Equal(0, snap.TransportReceiveErrors);
    }

    [Fact]
    public void Snapshot_ToString_ContainsKeyMetrics()
    {
        var diag = new AudioDiagnostics();
        diag.RecordCapture();
        diag.RecordSend();

        var snap = diag.GetSnapshot(isActive: true, isMuted: false, 3, 1);
        var text = snap.ToString();

        Assert.Contains("Active=True", text);
        Assert.Contains("Muted=False", text);
        Assert.Contains("Cap=1", text);
        Assert.Contains("Sent=1", text);
        Assert.Contains("JBFrames=3", text);
        Assert.Contains("Participants=1", text);
    }

    [Fact]
    public void ConcurrentIncrements_AreThreadSafe()
    {
        var diag = new AudioDiagnostics();
        const int iterations = 10_000;

        Parallel.For(0, iterations, _ =>
        {
            diag.RecordCapture();
            diag.RecordEncode();
            diag.RecordSend();
        });

        var snap = diag.GetSnapshot(false, false, 0, 0);
        Assert.Equal(iterations, snap.FramesCaptured);
        Assert.Equal(iterations, snap.FramesEncoded);
        Assert.Equal(iterations, snap.PacketsSent);
    }
}
