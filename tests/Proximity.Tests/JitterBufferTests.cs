using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Audio.Pipeline;

namespace Proximity.Tests;

public class JitterBufferTests
{
    private const int BufferDepthMs = 60;
    private const int FrameSizeMs = 20;

    [Fact]
    public void Constructor_SetsBufferDepth()
    {
        var buffer = CreateBuffer();

        Assert.Equal(3, buffer.BufferDepthFrames); // 60ms / 20ms = 3 frames
        Assert.False(buffer.IsPrimed);
        Assert.Equal(0, buffer.BufferedFrames);
    }

    [Fact]
    public void AddPacket_IncreasesBufferedFrames()
    {
        var buffer = CreateBuffer();
        var samples = new short[960];

        buffer.AddPacket(0, samples);

        Assert.Equal(1, buffer.BufferedFrames);
    }

    [Fact]
    public void AddPacket_PrimesAfterEnoughFrames()
    {
        var buffer = CreateBuffer();
        var samples = new short[960];

        buffer.AddPacket(0, samples);
        Assert.False(buffer.IsPrimed);

        buffer.AddPacket(1, samples);
        Assert.False(buffer.IsPrimed);

        buffer.AddPacket(2, samples);
        Assert.True(buffer.IsPrimed); // 3 frames = 60ms buffer
    }

    [Fact]
    public void GetNextFrame_ReturnsNull_WhenNotPrimed()
    {
        var buffer = CreateBuffer();
        var samples = new short[960];

        buffer.AddPacket(0, samples);

        var frame = buffer.GetNextFrame(out bool isMissing);

        Assert.Null(frame);
        Assert.False(isMissing);
    }

    [Fact]
    public void GetNextFrame_ReturnsFramesInOrder()
    {
        var buffer = CreateBuffer();

        var frame0 = CreateTestFrame(100);
        var frame1 = CreateTestFrame(200);
        var frame2 = CreateTestFrame(300);

        buffer.AddPacket(0, frame0);
        buffer.AddPacket(1, frame1);
        buffer.AddPacket(2, frame2);

        // Buffer is now primed, should return in order
        var result0 = buffer.GetNextFrame(out _);
        var result1 = buffer.GetNextFrame(out _);
        var result2 = buffer.GetNextFrame(out _);

        Assert.Equal(100, result0![0]);
        Assert.Equal(200, result1![0]);
        Assert.Equal(300, result2![0]);
    }

    [Fact]
    public void GetNextFrame_HandlesOutOfOrderPackets()
    {
        var buffer = CreateBuffer();

        var frame0 = CreateTestFrame(100);
        var frame1 = CreateTestFrame(200);
        var frame2 = CreateTestFrame(300);

        // Add out of order
        buffer.AddPacket(2, frame2);
        buffer.AddPacket(0, frame0);
        buffer.AddPacket(1, frame1);

        // Should still return in sequence order
        var result0 = buffer.GetNextFrame(out _);
        var result1 = buffer.GetNextFrame(out _);
        var result2 = buffer.GetNextFrame(out _);

        Assert.Equal(100, result0![0]);
        Assert.Equal(200, result1![0]);
        Assert.Equal(300, result2![0]);
    }

    [Fact]
    public void GetNextFrame_IndicatesMissingPacket()
    {
        var buffer = CreateBuffer();

        var frame0 = CreateTestFrame(100);
        var frame2 = CreateTestFrame(300);
        var frame3 = CreateTestFrame(400);

        // Add frames but skip sequence 1
        buffer.AddPacket(0, frame0);
        buffer.AddPacket(2, frame2);
        buffer.AddPacket(3, frame3);

        var result0 = buffer.GetNextFrame(out bool missing0);
        Assert.NotNull(result0);
        Assert.False(missing0);

        // Sequence 1 is missing
        var result1 = buffer.GetNextFrame(out bool missing1);
        Assert.Null(result1);
        Assert.True(missing1);

        // Sequence 2 should be available
        var result2 = buffer.GetNextFrame(out bool missing2);
        Assert.NotNull(result2);
        Assert.False(missing2);
        Assert.Equal(300, result2![0]);
    }

    [Fact]
    public void Reset_ClearsBuffer()
    {
        var buffer = CreateBuffer();
        var samples = new short[960];

        buffer.AddPacket(0, samples);
        buffer.AddPacket(1, samples);
        buffer.AddPacket(2, samples);
        Assert.True(buffer.IsPrimed);

        buffer.Reset();

        Assert.False(buffer.IsPrimed);
        Assert.Equal(0, buffer.BufferedFrames);
    }

    [Fact]
    public void BufferDepth_MinimumIsOneFrame()
    {
        // Even with very small buffer depth, minimum is 1 frame
        var buffer = new JitterBuffer(NullLogger.Instance, bufferDepthMs: 5, frameSizeMs: 20);
        Assert.Equal(1, buffer.BufferDepthFrames);
    }

    [Fact]
    public void MultipleSequences_FlowCorrectly()
    {
        var buffer = CreateBuffer();

        // Simulate streaming: add 6 frames, read them all
        for (uint i = 0; i < 6; i++)
        {
            buffer.AddPacket(i, CreateTestFrame((short)(i * 100)));
        }

        for (int i = 0; i < 6; i++)
        {
            var frame = buffer.GetNextFrame(out bool missing);
            Assert.NotNull(frame);
            Assert.False(missing);
            Assert.Equal((short)(i * 100), frame![0]);
        }
    }

    private static JitterBuffer CreateBuffer()
    {
        return new JitterBuffer(NullLogger.Instance, BufferDepthMs, FrameSizeMs);
    }

    private static short[] CreateTestFrame(short value)
    {
        var frame = new short[960];
        frame[0] = value;
        return frame;
    }
}
