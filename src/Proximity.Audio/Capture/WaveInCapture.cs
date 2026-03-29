using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Proximity.Core.Interfaces;

namespace Proximity.Audio.Capture;

/// <summary>
/// Audio capture implementation using NAudio WaveInEvent for microphone input.
/// Captures PCM audio at the configured sample rate and frame size.
/// </summary>
public class WaveInCapture : IAudioCapture
{
    private readonly ILogger _logger;
    private WaveInEvent? _waveIn;
    private bool _disposed;

    public bool IsCapturing { get; private set; }

    public event EventHandler<AudioDataEventArgs>? AudioDataAvailable;

    /// <summary>
    /// Create a WaveIn audio capture instance
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="deviceNumber">WaveIn device number (-1 for default)</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="frameSizeSamples">Frame size in samples for buffer sizing</param>
    public WaveInCapture(ILogger logger, int deviceNumber = -1, int sampleRate = 48000, int channels = 1, int frameSizeSamples = 960)
    {
        _logger = logger;

        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(sampleRate, 16, channels),
            BufferMilliseconds = Math.Max(10, frameSizeSamples * 1000 / sampleRate)
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _logger.LogDebug("WaveIn capture created: device={Device}, {Rate}Hz, {Ch}ch, buffer={BufMs}ms",
            deviceNumber, sampleRate, channels, _waveIn.BufferMilliseconds);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsCapturing) return;

        try
        {
            _waveIn?.StartRecording();
            IsCapturing = true;
            _logger.LogInformation("Audio capture started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio capture");
            throw;
        }
    }

    public void Stop()
    {
        if (!IsCapturing) return;

        try
        {
            _waveIn?.StopRecording();
            IsCapturing = false;
            _logger.LogInformation("Audio capture stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping audio capture");
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded > 0)
        {
            var buffer = new byte[e.BytesRecorded];
            Buffer.BlockCopy(e.Buffer, 0, buffer, 0, e.BytesRecorded);
            AudioDataAvailable?.Invoke(this, new AudioDataEventArgs(buffer, e.BytesRecorded));
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        IsCapturing = false;
        if (e.Exception != null)
        {
            _logger.LogError(e.Exception, "Audio capture stopped due to error");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Stop();

            if (_waveIn != null)
            {
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.RecordingStopped -= OnRecordingStopped;
                _waveIn.Dispose();
                _waveIn = null;
            }

            _logger.LogDebug("WaveIn capture disposed");
        }
    }
}
