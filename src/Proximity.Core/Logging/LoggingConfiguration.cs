using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Proximity.Core.Logging;

/// <summary>
/// Configures Serilog logging for the application
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configure Serilog with console and file sinks
    /// </summary>
    public static ILogger ConfigureLogging(IConfiguration configuration)
    {
        var logFilePath = configuration["Logging:LogFilePath"] ?? "logs/proximity-.log";
        var minimumLevel = ParseLogLevel(configuration["Logging:MinimumLevel"] ?? "Information");

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30)
            .CreateLogger();
    }

    private static LogEventLevel ParseLogLevel(string level)
    {
        return level.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}
