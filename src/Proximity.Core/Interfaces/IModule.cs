namespace Proximity.Core.Interfaces;

/// <summary>
/// Interface for modular components in the application
/// </summary>
public interface IModule
{
    /// <summary>
    /// Module name for identification
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// Initialize the module
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Start the module
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stop the module
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Clean up module resources
    /// </summary>
    Task DisposeAsync();
}
