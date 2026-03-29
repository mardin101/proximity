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
    private long _callbackCount;
    private long _totalBytesRecorded;

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
        var count = Interlocked.Increment(ref _callbackCount);
        var totalBytes = Interlocked.Add(ref _totalBytesRecorded, e.BytesRecorded);

        if (e.BytesRecorded > 0)
        {
            // Compute signal statistics for diagnostics
            int sampleCount = e.BytesRecorded / 2;
            short minSample = short.MaxValue, maxSample = short.MinValue;
            long sumSquares = 0;
            for (int i = 0; i < e.BytesRecorded - 1; i += 2)
            {
                short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
                if (sample < minSample) minSample = sample;
                if (sample > maxSample) maxSample = sample;
                sumSquares += (long)sample * sample;
            }
            double rms = sampleCount > 0 ? Math.Sqrt((double)sumSquares / sampleCount) : 0;
            bool isSilent = maxSample == 0 && minSample == 0;

            int subscriberCount = AudioDataAvailable?.GetInvocationList().Length ?? 0;

            // Log every callback at first, then periodically
            if (count <= 5 || count % 500 == 0)
            {
                _logger.LogInformation(
                    "[WaveIn] Callback #{Count}: {Bytes}B ({Samples} samples), " +
                    "signal min={Min} max={Max} RMS={RMS:F1} silent={Silent}, " +
                    "subscribers={Subscribers}, totalBytes={TotalBytes}",
                    count, e.BytesRecorded, sampleCount,
                    minSample, maxSample, rms, isSilent,
                    subscriberCount, totalBytes);
            }

            if (isSilent && count <= 50)
            {
                _logger.LogWarning("[WaveIn] Callback #{Count}: captured audio is all zeros (silent) — mic may not be providing data", count);
            }

            var buffer = new byte[e.BytesRecorded];
            Buffer.BlockCopy(e.Buffer, 0, buffer, 0, e.BytesRecorded);
            AudioDataAvailable?.Invoke(this, new AudioDataEventArgs(buffer, e.BytesRecorded));
        }
        else
        {
            _logger.LogWarning("[WaveIn] Callback #{Count}: BytesRecorded=0 — no audio data from device", count);
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
