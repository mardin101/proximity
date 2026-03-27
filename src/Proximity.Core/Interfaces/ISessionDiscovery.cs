using Proximity.Core.Models;

namespace Proximity.Core.Interfaces;

/// <summary>
/// Interface for discovering voice sessions on the local network
/// </summary>
public interface ISessionDiscovery : IAsyncDisposable
{
    /// <summary>
    /// Start broadcasting session announcements (host mode)
    /// </summary>
    Task StartBroadcastingAsync(VoiceSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop broadcasting session announcements
    /// </summary>
    Task StopBroadcastingAsync();

    /// <summary>
    /// Start listening for session announcements (client mode)
    /// </summary>
    Task StartDiscoveryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop listening for session announcements
    /// </summary>
    Task StopDiscoveryAsync();

    /// <summary>
    /// Event raised when a new session is discovered
    /// </summary>
    event EventHandler<VoiceSession> SessionDiscovered;

    /// <summary>
    /// Event raised when a session is no longer available
    /// </summary>
    event EventHandler<Guid> SessionLost;
}
