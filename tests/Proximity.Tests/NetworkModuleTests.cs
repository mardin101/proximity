using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proximity.Network;

namespace Proximity.Tests;

public class NetworkModuleTests
{
    [Fact]
    public async Task InitializeAsync_CreatesComponents()
    {
        var loggerFactory = new NullLoggerFactory();
        var module = new NetworkModule(
            NullLogger<NetworkModule>.Instance,
            loggerFactory);

        await module.InitializeAsync();

        Assert.NotNull(module.Discovery);
        Assert.NotNull(module.SessionManager);
        Assert.NotNull(module.AudioTransport);
    }

    [Fact]
    public void ModuleName_IsNetwork()
    {
        var module = new NetworkModule(
            NullLogger<NetworkModule>.Instance,
            new NullLoggerFactory());

        Assert.Equal("Network", module.ModuleName);
    }

    [Fact]
    public async Task FullLifecycle_DoesNotThrow()
    {
        var module = new NetworkModule(
            NullLogger<NetworkModule>.Instance,
            new NullLoggerFactory());

        await module.InitializeAsync();
        await module.StartAsync();
        await module.StopAsync();
        await module.DisposeAsync();
    }
}
