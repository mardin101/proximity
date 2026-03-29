namespace Proximity.Audio.Pipeline;

/// <summary>
/// Audio mixer that combines multiple PCM audio streams into a single output stream.
/// Handles per-participant volume control and audio clipping prevention.
/// </summary>
public class AudioMixer
{
    private readonly Dictionary<Guid, float> _volumes = new();
    private readonly object _lock = new();

    /// <summary>
    /// Set the volume for a specific participant (0.0 to 1.0)
    /// </summary>
    public void SetVolume(Guid participantId, float volume)
    {
        volume = Math.Clamp(volume, 0.0f, 1.0f);
        lock (_lock)
        {
            _volumes[participantId] = volume;
        }
    }

    /// <summary>
    /// Get the volume for a specific participant
    /// </summary>
    public float GetVolume(Guid participantId)
    {
        lock (_lock)
        {
            return _volumes.TryGetValue(participantId, out var volume) ? volume : 1.0f;
        }
    }

    /// <summary>
    /// Remove a participant from the mixer
    /// </summary>
    public void RemoveParticipant(Guid participantId)
    {
        lock (_lock)
        {
            _volumes.Remove(participantId);
        }
    }

    /// <summary>
    /// Mix multiple audio streams into a single output buffer.
    /// Applies per-participant volume and clips the result to prevent distortion.
    /// </summary>
    /// <param name="streams">Dictionary of participant ID to their PCM samples</param>
    /// <param name="frameSize">Number of samples per frame</param>
    /// <returns>Mixed PCM samples</returns>
    public short[] Mix(Dictionary<Guid, short[]> streams, int frameSize)
    {
        if (streams.Count == 0)
        {
            return new short[frameSize];
        }

        if (streams.Count == 1)
        {
            var single = streams.First();
            float volume = GetVolume(single.Key);
            return ApplyVolume(single.Value, frameSize, volume);
        }

        var mixed = new int[frameSize];

        foreach (var (participantId, samples) in streams)
        {
            float volume = GetVolume(participantId);
            int sampleCount = Math.Min(samples.Length, frameSize);

            for (int i = 0; i < sampleCount; i++)
            {
                mixed[i] += (int)(samples[i] * volume);
            }
        }

        // Clip to 16-bit range
        var output = new short[frameSize];
        for (int i = 0; i < frameSize; i++)
        {
            output[i] = (short)Math.Clamp(mixed[i], short.MinValue, short.MaxValue);
        }

        return output;
    }

    /// <summary>
    /// Convert PCM samples (16-bit signed) to byte array (little-endian)
    /// </summary>
    public static byte[] SamplesToBytes(short[] samples)
    {
        var bytes = new byte[samples.Length * 2];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Convert byte array (little-endian) to PCM samples (16-bit signed)
    /// </summary>
    public static short[] BytesToSamples(byte[] bytes, int offset, int count)
    {
        int sampleCount = count / 2;
        var samples = new short[sampleCount];
        Buffer.BlockCopy(bytes, offset, samples, 0, count);
        return samples;
    }

    private static short[] ApplyVolume(short[] samples, int frameSize, float volume)
    {
        int count = Math.Min(samples.Length, frameSize);
        var output = new short[frameSize];

        if (Math.Abs(volume - 1.0f) < 0.001f)
        {
            Array.Copy(samples, output, count);
            return output;
        }

        for (int i = 0; i < count; i++)
        {
            output[i] = (short)Math.Clamp((int)(samples[i] * volume), short.MinValue, short.MaxValue);
        }

        return output;
    }
}
