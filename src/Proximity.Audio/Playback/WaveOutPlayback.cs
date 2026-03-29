using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Proximity.Audio.Pipeline;
using Proximity.Core.Interfaces;

namespace Proximity.Audio.Playback;

/// <summary>
/// Audio playback implementation using NAudio WaveOutEvent with multi-participant mixing.
/// Receives audio from multiple participants, mixes them together, and plays through the output device.
/// </summary>
public class WaveOutPlayback : IAudioPlayback
{
    private readonly ILogger _logger;
    private readonly AudioMixer _mixer;
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly int _frameSize;

    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _bufferedProvider;
    private readonly ConcurrentDictionary<Guid, Queue<short[]>> _participantBuffers = new();
    private bool _disposed;
    private bool _isPlaying;

    /// <summary>
    /// Create a WaveOut audio playback instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="mixer">Audio mixer for combining streams</param>
    /// <param name="deviceNumber">WaveOut device number (-1 for default)</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="frameSize">Frame size in samples</param>
    public WaveOutPlayback(ILogger logger, AudioMixer mixer, int deviceNumber = -1, int sampleRate = 48000, int channels = 1, int frameSize = 960)
    {
        _logger = logger;
        _mixer = mixer;
        _sampleRate = sampleRate;
        _channels = channels;
        _frameSize = frameSize;

        var waveFormat = new WaveFormat(sampleRate, 16, channels);

        _bufferedProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferDuration = TimeSpan.FromMilliseconds(200),
            DiscardOnBufferOverflow = true
        };

        _waveOut = new WaveOutEvent
        {
            DeviceNumber = deviceNumber,
            DesiredLatency = 50,
            NumberOfBuffers = 3
        };

        _waveOut.Init(_bufferedProvider);

        _logger.LogDebug("WaveOut playback created: device={Device}, {Rate}Hz, {Ch}ch, frame={Frame}",
            deviceNumber, sampleRate, channels, frameSize);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isPlaying) return;

        try
        {
            _waveOut?.Play();
            _isPlaying = true;
            _logger.LogInformation("Audio playback started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio playback");
            throw;
        }
    }

    public void Stop()
    {
        if (!_isPlaying) return;

        try
        {
            _waveOut?.Stop();
            _isPlaying = false;
            _bufferedProvider?.ClearBuffer();
            _participantBuffers.Clear();
            _logger.LogInformation("Audio playback stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping audio playback");
        }
    }

    public void AddSamples(Guid participantId, byte[] audioData, int offset, int count)
    {
        if (_disposed || !_isPlaying) return;

        try
        {
            // Feed mixed audio bytes directly to the buffered provider
            _bufferedProvider?.AddSamples(audioData, offset, count);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error adding audio samples for {ParticipantId}", participantId);
        }
    }

    public void SetParticipantVolume(Guid participantId, float volume)
    {
        _mixer.SetVolume(participantId, volume);
    }

    public void RemoveParticipant(Guid participantId)
    {
        _mixer.RemoveParticipant(participantId);
        _participantBuffers.TryRemove(participantId, out _);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Stop();

            if (_waveOut != null)
            {
                _waveOut.Dispose();
                _waveOut = null;
            }

            _bufferedProvider = null;
            _participantBuffers.Clear();

            _logger.LogDebug("WaveOut playback disposed");
        }
    }
}
