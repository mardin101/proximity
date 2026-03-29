using Concentus;
using Concentus.Enums;
using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;

namespace Proximity.Audio.Codec;

/// <summary>
/// Opus codec wrapper using Concentus for low-latency, high-quality audio compression.
/// Supports encoding PCM to Opus and decoding Opus back to PCM with packet loss concealment.
/// </summary>
public class OpusCodecWrapper : IAudioCodec
{
    private readonly ILogger<OpusCodecWrapper> _logger;
    private readonly IOpusEncoder _encoder;
    private readonly IOpusDecoder _decoder;
    private readonly int _frameSize;
    private readonly int _sampleRate;
    private readonly int _channels;
    private bool _disposed;

    // Maximum Opus packet size (recommended by RFC 6716)
    private const int MaxPacketSize = 4000;

    public int FrameSize => _frameSize;
    public int SampleRate => _sampleRate;
    public int Channels => _channels;

    /// <summary>
    /// Create an Opus codec wrapper with specified audio parameters
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="sampleRate">Sample rate in Hz (8000, 12000, 16000, 24000, or 48000)</param>
    /// <param name="channels">Number of channels (1 = mono, 2 = stereo)</param>
    /// <param name="frameSize">Frame size in samples (e.g., 960 for 20ms @ 48kHz)</param>
    /// <param name="bitrate">Target bitrate in bits per second</param>
    public OpusCodecWrapper(ILogger<OpusCodecWrapper> logger, int sampleRate = 48000, int channels = 1, int frameSize = 960, int bitrate = 32000)
    {
        _logger = logger;
        _sampleRate = sampleRate;
        _channels = channels;
        _frameSize = frameSize;

        _encoder = OpusCodecFactory.CreateEncoder(sampleRate, channels, OpusApplication.OPUS_APPLICATION_VOIP);
        _encoder.Bitrate = bitrate;
        _encoder.Complexity = 5; // Balance between quality and CPU
        _encoder.UseInbandFEC = true; // Forward error correction
        _encoder.PacketLossPercent = 5; // Expected packet loss

        _decoder = OpusCodecFactory.CreateDecoder(sampleRate, channels);

        _logger.LogInformation("Opus codec initialized: {SampleRate}Hz, {Channels}ch, {FrameSize} samples/frame, {Bitrate}bps",
            sampleRate, channels, frameSize, bitrate);
    }

    public byte[] Encode(short[] pcmSamples, int sampleCount)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Span<byte> outputBuffer = stackalloc byte[MaxPacketSize];
        int encodedLength = _encoder.Encode(pcmSamples.AsSpan(0, _frameSize * _channels), _frameSize, outputBuffer, MaxPacketSize);

        return outputBuffer[..encodedLength].ToArray();
    }

    public short[] Decode(byte[] encodedData, int encodedLength)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var outputBuffer = new short[_frameSize * _channels];
        int decodedSamples = _decoder.Decode(encodedData.AsSpan(0, encodedLength), outputBuffer.AsSpan(), _frameSize, false);

        if (decodedSamples != _frameSize)
        {
            _logger.LogDebug("Decoded {Samples} samples, expected {Expected}", decodedSamples, _frameSize);
        }

        return outputBuffer;
    }

    public short[] DecodePLC()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var outputBuffer = new short[_frameSize * _channels];
        _decoder.Decode(ReadOnlySpan<byte>.Empty, outputBuffer.AsSpan(), _frameSize, true);

        return outputBuffer;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _encoder.ResetState();
            _decoder.ResetState();
            _logger.LogDebug("Opus codec disposed");
        }
    }
}
