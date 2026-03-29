using Proximity.Audio.Pipeline;

namespace Proximity.Audio.Diagnostics;

/// <summary>
/// Generates PCM audio test tones (sine wave) for verifying output devices
/// and codec paths without requiring a microphone or network.
/// </summary>
public static class ToneGenerator
{
    /// <summary>
    /// Generate a sine-wave tone as 16-bit PCM samples.
    /// </summary>
    /// <param name="frequencyHz">Tone frequency in Hz (e.g., 440 for A4)</param>
    /// <param name="durationMs">Duration in milliseconds</param>
    /// <param name="sampleRate">Sample rate in Hz (default 48000)</param>
    /// <param name="amplitude">Amplitude 0.0–1.0 (default 0.5)</param>
    /// <returns>PCM samples as 16-bit signed integers</returns>
    public static short[] GenerateSineTone(int frequencyHz, int durationMs, int sampleRate = 48000, double amplitude = 0.5)
    {
        if (frequencyHz <= 0) throw new ArgumentOutOfRangeException(nameof(frequencyHz), "Frequency must be positive.");
        if (durationMs <= 0) throw new ArgumentOutOfRangeException(nameof(durationMs), "Duration must be positive.");
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");
        amplitude = Math.Clamp(amplitude, 0.0, 1.0);

        int totalSamples = sampleRate * durationMs / 1000;
        var samples = new short[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;
            samples[i] = (short)(Math.Sin(2.0 * Math.PI * frequencyHz * t) * short.MaxValue * amplitude);
        }

        return samples;
    }

    /// <summary>
    /// Generate a sine-wave tone and return it as raw bytes suitable for
    /// <see cref="IAudioPlayback.AddSamples"/>.
    /// </summary>
    public static byte[] GenerateSineToneBytes(int frequencyHz, int durationMs, int sampleRate = 48000, double amplitude = 0.5)
    {
        var samples = GenerateSineTone(frequencyHz, durationMs, sampleRate, amplitude);
        return AudioMixer.SamplesToBytes(samples);
    }

    /// <summary>
    /// Split a PCM sample array into frame-sized chunks.
    /// The last chunk is zero-padded if it doesn't fill a complete frame.
    /// </summary>
    /// <param name="samples">Input PCM samples</param>
    /// <param name="frameSize">Samples per frame (e.g., 960 for 20 ms @ 48 kHz)</param>
    /// <returns>List of frame-sized sample arrays</returns>
    public static List<short[]> SplitIntoFrames(short[] samples, int frameSize)
    {
        if (frameSize <= 0) throw new ArgumentOutOfRangeException(nameof(frameSize), "Frame size must be positive.");

        var frames = new List<short[]>();
        int offset = 0;

        while (offset < samples.Length)
        {
            var frame = new short[frameSize];
            int toCopy = Math.Min(frameSize, samples.Length - offset);
            Array.Copy(samples, offset, frame, 0, toCopy);
            // Remaining samples in frame are zero (silence) if toCopy < frameSize
            frames.Add(frame);
            offset += frameSize;
        }

        return frames;
    }
}
