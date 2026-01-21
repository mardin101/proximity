namespace Proximity.Core.Configuration;

/// <summary>
/// Application settings loaded from configuration
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Network-related settings
    /// </summary>
    public NetworkSettings Network { get; set; } = new();

    /// <summary>
    /// Audio-related settings
    /// </summary>
    public AudioSettings Audio { get; set; } = new();

    /// <summary>
    /// Logging settings
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();
}

/// <summary>
/// Network configuration settings
/// </summary>
public class NetworkSettings
{
    /// <summary>
    /// Port for network communication
    /// </summary>
    public int Port { get; set; } = 7777;

    /// <summary>
    /// Maximum number of connections
    /// </summary>
    public int MaxConnections { get; set; } = 10;
}

/// <summary>
/// Audio configuration settings
/// </summary>
public class AudioSettings
{
    /// <summary>
    /// Sample rate for audio
    /// </summary>
    public int SampleRate { get; set; } = 48000;

    /// <summary>
    /// Audio buffer size
    /// </summary>
    public int BufferSize { get; set; } = 1024;
}

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Minimum log level
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Log file path
    /// </summary>
    public string LogFilePath { get; set; } = "logs/proximity-.log";
}
