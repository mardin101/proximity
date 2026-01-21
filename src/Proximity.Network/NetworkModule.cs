using Microsoft.Extensions.Logging;
using Proximity.Core.Interfaces;

namespace Proximity.Network;

/// <summary>
/// Network module for handling network communication
/// </summary>
public class NetworkModule : IModule
{
    private readonly ILogger<NetworkModule> _logger;

    public string ModuleName => "Network";

    public NetworkModule(ILogger<NetworkModule> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        _logger.LogInformation("Network module initializing...");
        // TODO: Initialize network sockets and connections
        return Task.CompletedTask;
    }

    public Task StartAsync()
    {
        _logger.LogInformation("Network module starting...");
        // TODO: Start listening for connections
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _logger.LogInformation("Network module stopping...");
        // TODO: Close connections and stop listening
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _logger.LogInformation("Network module disposing...");
        // TODO: Clean up network resources
        return Task.CompletedTask;
    }
}
