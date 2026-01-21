using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proximity.Audio;
using Proximity.Core.Configuration;
using Proximity.Core.Interfaces;
using Proximity.Core.Logging;
using Proximity.Network;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Serilog.ILogger;

namespace Proximity.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private ILogger? _logger;
    private readonly List<IModule> _modules = new();
    private readonly HashSet<IModule> _initializedModules = new();
    private readonly HashSet<IModule> _startedModules = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Build configuration
            var configuration = BuildConfiguration();

            // Configure Serilog
            _logger = LoggingConfiguration.ConfigureLogging(configuration);
            Log.Logger = _logger;

            _logger.Information("=== Proximity Application Starting ===");
            _logger.Information("Application startup initiated");

            // Build host with dependency injection
            _host = BuildHost(configuration);

            _logger.Information("Dependency injection container configured");

            // Get modules from DI container
            var audioModule = _host.Services.GetRequiredService<AudioModule>();
            var networkModule = _host.Services.GetRequiredService<NetworkModule>();
            _modules.Add(audioModule);
            _modules.Add(networkModule);

            // Initialize all modules
            foreach (var module in _modules)
            {
                await module.InitializeAsync();
                _initializedModules.Add(module);
            }

            // Start all modules
            foreach (var module in _modules)
            {
                await module.StartAsync();
                _startedModules.Add(module);
            }

            _logger.Information("All modules started successfully");

            // Show main window
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            _logger.Information("Main window displayed");
        }
        catch (Exception ex)
        {
            _logger?.Fatal(ex, "Fatal error during application startup");
            MessageBox.Show($"Fatal error during startup: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.Information("Application shutdown initiated");

            // Stop all started modules (in reverse order)
            foreach (var module in _startedModules.Reverse())
            {
                try
                {
                    await module.StopAsync();
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "Error stopping module {ModuleName}", module.ModuleName);
                }
            }

            // Dispose all initialized modules (in reverse order)
            foreach (var module in _initializedModules.Reverse())
            {
                try
                {
                    await module.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "Error disposing module {ModuleName}", module.ModuleName);
                }
            }

            _logger?.Information("All modules stopped and disposed");
            _logger?.Information("=== Proximity Application Shutdown ===");

            // Dispose host
            _host?.Dispose();
            Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Error during application shutdown");
        }

        base.OnExit(e);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    private static IHost BuildHost(IConfiguration configuration)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.AddSingleton(configuration);
                services.Configure<AppSettings>(configuration);

                // Register logging
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog(dispose: true);
                });

                // Register modules
                services.AddSingleton<AudioModule>();
                services.AddSingleton<NetworkModule>();

                // Register main window
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }
}

