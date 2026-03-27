namespace Proximity.Core.Interfaces;

/// <summary>
/// Interface for sending and receiving audio data over the network (UDP)
/// </summary>
public interface IAudioTransport : IAsyncDisposable
{
    /// <summary>
    /// Start the audio transport on the specified port
    /// </summary>
    Task StartAsync(int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the audio transport
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Send audio data to a specific endpoint
    /// </summary>
    Task SendAudioAsync(Guid senderId, byte[] audioData, int count, string remoteAddress, int remotePort);

    /// <summary>
    /// Add a target endpoint to send audio to
    /// </summary>
    void AddTarget(Guid participantId, string address, int port);

    /// <summary>
    /// Remove a target endpoint
    /// </summary>
    void RemoveTarget(Guid participantId);

    /// <summary>
    /// Send audio to all registered targets
    /// </summary>
    Task BroadcastAudioAsync(Guid senderId, byte[] audioData, int count);

    /// <summary>
    /// Event raised when audio data is received
    /// </summary>
    event EventHandler<AudioPacketEventArgs> AudioReceived;
}

/// <summary>
/// Event args for received audio packets
/// </summary>
public class AudioPacketEventArgs : EventArgs
{
    /// <summary>
    /// ID of the sender
    /// </summary>
    public Guid SenderId { get; }

    /// <summary>
    /// Audio data
    /// </summary>
    public byte[] AudioData { get; }

    /// <summary>
    /// Length of valid audio data
    /// </summary>
    public int Length { get; }

    public AudioPacketEventArgs(Guid senderId, byte[] audioData, int length)
    {
        SenderId = senderId;
        AudioData = audioData;
        Length = length;
    }
}
