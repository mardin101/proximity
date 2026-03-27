using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;
using Proximity.Network.Discovery;
using Proximity.Network.Session;
using Proximity.Network.Transport;

namespace Proximity.Network;

/// <summary>
/// Network module for handling session discovery, management, and audio transport
/// </summary>
public class NetworkModule : IModule
{
    private readonly ILogger<NetworkModule> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public string ModuleName => "Network";

    public UdpSessionDiscovery? Discovery { get; private set; }
    public SessionManager? SessionManager { get; private set; }
    public UdpAudioTransport? AudioTransport { get; private set; }

    public NetworkModule(ILogger<NetworkModule> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public Task InitializeAsync()
    {
        _logger.LogInformation("Network module initializing...");

        Discovery = new UdpSessionDiscovery(_loggerFactory.CreateLogger<UdpSessionDiscovery>());
        SessionManager = new SessionManager(_loggerFactory.CreateLogger<SessionManager>());
        AudioTransport = new UdpAudioTransport(_loggerFactory.CreateLogger<UdpAudioTransport>());

        _logger.LogInformation("Network module initialized with discovery, session manager, and audio transport");
        return Task.CompletedTask;
    }

    public Task StartAsync()
    {
        _logger.LogInformation("Network module started");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _logger.LogInformation("Network module stopping...");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _logger.LogInformation("Network module disposing...");

        if (AudioTransport != null)
            await AudioTransport.DisposeAsync();

        if (SessionManager != null)
            await SessionManager.DisposeAsync();

        if (Discovery != null)
            await Discovery.DisposeAsync();
    }
}
