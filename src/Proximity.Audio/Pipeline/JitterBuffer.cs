using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Proximity.Audio.Pipeline;

/// <summary>
/// Jitter buffer for reordering out-of-order audio packets and handling packet loss.
/// Buffers incoming packets and delivers them in sequence order, using packet loss
/// concealment (PLC) for missing frames.
/// </summary>
public class JitterBuffer
{
    private readonly ILogger _logger;
    private readonly int _bufferDepthFrames;
    private readonly ConcurrentDictionary<uint, short[]> _buffer = new();
    private readonly object _lock = new();

    private uint _nextPlaybackSequence;
    private uint _highestReceivedSequence;
    private bool _primed;
    private int _receivedCount;

    /// <summary>
    /// Number of frames currently buffered
    /// </summary>
    public int BufferedFrames => _buffer.Count;

    /// <summary>
    /// Number of frames that must be buffered before playback starts
    /// </summary>
    public int BufferDepthFrames => _bufferDepthFrames;

    /// <summary>
    /// Whether the buffer has been primed (enough frames for playback)
    /// </summary>
    public bool IsPrimed => _primed;

    /// <summary>
    /// Create a jitter buffer with the specified depth
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="bufferDepthMs">Buffer depth in milliseconds</param>
    /// <param name="frameSizeMs">Frame size in milliseconds</param>
    public JitterBuffer(ILogger logger, int bufferDepthMs = 60, int frameSizeMs = 20)
    {
        _logger = logger;
        _bufferDepthFrames = Math.Max(1, bufferDepthMs / frameSizeMs);
        _logger.LogDebug("Jitter buffer created: {Depth} frames ({DepthMs}ms), frame size {FrameMs}ms",
            _bufferDepthFrames, bufferDepthMs, frameSizeMs);
    }

    /// <summary>
    /// Add a packet to the jitter buffer
    /// </summary>
    /// <param name="sequenceNumber">Sequence number for ordering</param>
    /// <param name="pcmSamples">Decoded PCM audio samples</param>
    public void AddPacket(uint sequenceNumber, short[] pcmSamples)
    {
        lock (_lock)
        {
            // Track the lowest sequence number seen before priming
            if (_receivedCount == 0)
            {
                _nextPlaybackSequence = sequenceNumber;
            }
            else if (!_primed && sequenceNumber < _nextPlaybackSequence)
            {
                _nextPlaybackSequence = sequenceNumber;
            }

            _buffer[sequenceNumber] = pcmSamples;
            _receivedCount++;

            if (sequenceNumber > _highestReceivedSequence)
            {
                _highestReceivedSequence = sequenceNumber;
            }

            // Prime the buffer once we have enough frames
            if (!_primed && _buffer.Count >= _bufferDepthFrames)
            {
                _primed = true;
                _logger.LogDebug("Jitter buffer primed with {Count} frames", _buffer.Count);
            }

            // Discard old packets that are too far behind
            CleanupOldPackets();
        }
    }

    /// <summary>
    /// Get the next frame for playback.
    /// Returns the buffered frame if available, or null if not ready or missing.
    /// </summary>
    /// <param name="isMissing">Set to true if this frame was missing (needs PLC)</param>
    /// <returns>PCM samples, or null if buffer is not primed</returns>
    public short[]? GetNextFrame(out bool isMissing)
    {
        isMissing = false;

        lock (_lock)
        {
            if (!_primed)
            {
                return null;
            }

            if (_buffer.TryRemove(_nextPlaybackSequence, out var samples))
            {
                _nextPlaybackSequence++;
                return samples;
            }

            // If we've drained past the highest received sequence and the buffer
            // is empty, there are no more frames to play back.  Signal "not ready"
            // so the caller stops draining instead of generating PLC forever.
            if (_nextPlaybackSequence > _highestReceivedSequence && _buffer.IsEmpty)
            {
                return null;
            }

            // Frame is missing - advance sequence and signal PLC
            isMissing = true;
            _nextPlaybackSequence++;
            return null;
        }
    }

    /// <summary>
    /// Reset the jitter buffer state
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _buffer.Clear();
            _nextPlaybackSequence = 0;
            _highestReceivedSequence = 0;
            _primed = false;
            _receivedCount = 0;
            _logger.LogDebug("Jitter buffer reset");
        }
    }

    private void CleanupOldPackets()
    {
        // Remove packets that are too far behind the playback position.
        // 50 frames × 20ms = ~1 second of maximum lag tolerance.
        const int maxLag = 50;
        foreach (var key in _buffer.Keys)
        {
            if (key + maxLag < _nextPlaybackSequence)
            {
                _buffer.TryRemove(key, out _);
            }
        }
    }
}
