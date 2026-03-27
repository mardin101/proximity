namespace Proximity.Core.Interfaces;

/// <summary>
/// Interface for audio capture (microphone input)
/// </summary>
public interface IAudioCapture : IDisposable
{
    /// <summary>
    /// Start capturing audio from the microphone
    /// </summary>
    void Start();

    /// <summary>
    /// Stop capturing audio
    /// </summary>
    void Stop();

    /// <summary>
    /// Whether audio capture is currently active
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// Event raised when audio data is available
    /// </summary>
    event EventHandler<AudioDataEventArgs> AudioDataAvailable;
}

/// <summary>
/// Interface for audio playback (speaker output)
/// </summary>
public interface IAudioPlayback : IDisposable
{
    /// <summary>
    /// Start playback
    /// </summary>
    void Start();

    /// <summary>
    /// Stop playback
    /// </summary>
    void Stop();

    /// <summary>
    /// Add audio samples from a specific participant
    /// </summary>
    void AddSamples(Guid participantId, byte[] audioData, int offset, int count);

    /// <summary>
    /// Set the volume for a specific participant (0.0 to 1.0)
    /// </summary>
    void SetParticipantVolume(Guid participantId, float volume);

    /// <summary>
    /// Remove a participant's audio stream
    /// </summary>
    void RemoveParticipant(Guid participantId);
}

/// <summary>
/// Event args for audio data
/// </summary>
public class AudioDataEventArgs : EventArgs
{
    /// <summary>
    /// Raw audio data bytes
    /// </summary>
    public byte[] Buffer { get; }

    /// <summary>
    /// Number of bytes recorded
    /// </summary>
    public int BytesRecorded { get; }

    public AudioDataEventArgs(byte[] buffer, int bytesRecorded)
    {
        Buffer = buffer;
        BytesRecorded = bytesRecorded;
    }
}
