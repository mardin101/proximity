# Proximity - MVP Foundation

A modular Windows desktop application built with .NET and WPF, featuring a plugin-style architecture for rapid feature development.

## 🏗️ Architecture

The application follows a modular architecture with clear separation of concerns:

```
Proximity/
├── src/
│   ├── Proximity.App/         # WPF Application (Entry Point)
│   ├── Proximity.Core/        # Business Logic & Infrastructure
│   ├── Proximity.Audio/       # Audio Module
│   ├── Proximity.Network/     # Network Module
│   └── Proximity.UI/          # UI Components (WPF)
└── Proximity.sln              # Solution File
```

## ✨ Features Implemented

### Core Infrastructure
- ✅ **Modular Architecture**: Plugin-style components for Audio, Network, and UI
- ✅ **Logging Framework**: Serilog with console and file output
- ✅ **Configuration System**: JSON-based configuration with `appsettings.json`
- ✅ **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- ✅ **Application Lifecycle**: Proper startup, shutdown, and error handling
- ✅ **Portable Executable**: Runs without installation or admin rights

### Technology Stack
- **Framework**: .NET 10+
- **UI**: WPF (Windows Presentation Foundation)
- **Logging**: Serilog
- **Configuration**: Microsoft.Extensions.Configuration
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Architecture**: MVVM-ready with modular components

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK or later
- Windows 10/11 (for running the application)
- Visual Studio 2022 or VS Code (optional, for development)

### Building the Application

```bash
# Clone the repository
git clone https://github.com/mardin101/proximity.git
cd proximity

# Build the solution
dotnet build Proximity.sln

# Run the application
dotnet run --project src/Proximity.App/Proximity.App.csproj
```

### Build Output

The executable is located at:
```
src/Proximity.App/bin/Debug/net10.0-windows/Proximity.App.exe
```

This is a portable executable that can be copied and run without installation.

## ⚙️ Configuration

The application is configured via `appsettings.json`:

```json
{
  "Logging": {
    "MinimumLevel": "Information",
    "LogFilePath": "logs/proximity-.log"
  },
  "Network": {
    "Port": 7777,
    "MaxConnections": 10
  },
  "Audio": {
    "SampleRate": 48000,
    "BufferSize": 1024
  }
}
```

### Configuration Options

- **Logging.MinimumLevel**: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`
- **Logging.LogFilePath**: Path for log files (supports rolling by day)
- **Network.Port**: Port number for network communication
- **Network.MaxConnections**: Maximum concurrent connections
- **Audio.SampleRate**: Audio sample rate in Hz
- **Audio.BufferSize**: Audio buffer size in samples

## 📁 Project Structure

### Proximity.Core
Contains core business logic and infrastructure:
- **Configuration**: Application settings classes
- **Interfaces**: `IModule` interface for modular components
- **Logging**: Serilog configuration

### Proximity.Audio
Audio module for handling audio input/output:
- Implements `IModule` interface
- Ready for audio capture/playback implementation

### Proximity.Network
Network module for handling communication:
- Implements `IModule` interface
- Ready for socket/connection implementation

### Proximity.UI
WPF UI components and views:
- Shared UI components
- Can be used across different windows

### Proximity.App
Main application entry point:
- Application initialization
- Dependency injection setup
- Module lifecycle management
- Main window (WPF)

## 🔌 Module System

All modules implement the `IModule` interface:

```csharp
public interface IModule
{
    string ModuleName { get; }
    Task InitializeAsync();
    Task StartAsync();
    Task StopAsync();
    Task DisposeAsync();
}
```

### Adding a New Module

1. Create a new class library project
2. Reference `Proximity.Core`
3. Implement `IModule` interface
4. Register in `App.xaml.cs`:

```csharp
services.AddSingleton<YourModule>();
```

## 📝 Logging

Logs are written to:
- **Console**: Colored output during development
- **File**: `logs/proximity-YYYYMMDD.log` (rolling daily, 30 days retention)

Example log output:
```
[14:30:45 INF] === Proximity Application Starting ===
[14:30:45 INF] Application startup initiated
[14:30:45 INF] Dependency injection container configured
[14:30:45 INF] Audio module initializing...
[14:30:45 INF] Network module initializing...
```

## 🧪 Testing

To run tests (when implemented):

```bash
dotnet test
```

## 🛠️ Development

### Building for Release

```bash
dotnet build Proximity.sln -c Release
```

Release output will be in:
```
src/Proximity.App/bin/Release/net10.0-windows/
```

### Publishing Self-Contained

To create a self-contained executable with runtime:

```bash
dotnet publish src/Proximity.App/Proximity.App.csproj -c Release -r win-x64 --self-contained
```

## 📦 Dependencies

### NuGet Packages Used
- **Serilog** - Logging framework
- **Serilog.Sinks.Console** - Console logging
- **Serilog.Sinks.File** - File logging
- **Microsoft.Extensions.Configuration** - Configuration system
- **Microsoft.Extensions.Configuration.Json** - JSON configuration
- **Microsoft.Extensions.DependencyInjection** - DI container
- **Microsoft.Extensions.Hosting** - Application host
- **Serilog.Extensions.Logging** - Serilog integration with Microsoft.Extensions.Logging

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🎯 Roadmap

Future enhancements:
- Audio capture and playback implementation
- Network communication protocols
- User settings UI
- Voice activity detection
- Proximity-based audio features
- Testing infrastructure

## 🐛 Troubleshooting

### Application won't start
- Check logs in the `logs/` folder
- Ensure .NET 10+ runtime is installed
- Verify `appsettings.json` is present

### Build errors
- Ensure .NET SDK 10+ is installed: `dotnet --version`
- Restore NuGet packages: `dotnet restore`
- Clean and rebuild: `dotnet clean && dotnet build`

## 📞 Support

For issues and questions, please use the GitHub Issues page.
