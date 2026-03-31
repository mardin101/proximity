# ADR-004: Build Portable Executable Without Installation

**Status**: Proposed

**Date**: 2024-01-21

**Decision Makers**: Architecture Team, Development Team

**Tags**: deployment, distribution, portability, mvp

---

## Context

### Problem Statement

We need a deployment strategy for the Proximity MVP that:
- Minimizes friction for users (no installation wizard, admin rights, or prerequisites)
- Enables rapid iteration and distribution during MVP development
- Works reliably across Windows 10 and Windows 11
- Supports easy rollback if issues are discovered
- Allows running from any location (USB drive, network share, etc.)
- Simplifies development and testing (no packaging complexity)

Traditional installation approaches with MSI/EXE installers require:
- Administrative privileges
- Installation wizards (poor UX)
- Registry modifications
- Dealing with prerequisites (.NET runtime)
- Complex uninstall procedures
- Longer development cycle (packaging, signing, etc.)

### Background

The Proximity MVP is in rapid development with frequent releases for internal testing and early user feedback. The team needs to distribute updates quickly without elaborate deployment processes. Users should be able to try the application with minimal commitment.

Modern .NET supports self-contained deployments that include the runtime, eliminating prerequisites. The application stores user settings in %APPDATA%, which doesn't require admin privileges. This aligns well with a portable executable approach.

### Constraints

- **Target Platform**: Windows 10 (1809+) and Windows 11
- **User Permissions**: Cannot require administrative privileges
- **Distribution**: Simple file download (no complex package managers)
- **MVP Timeline**: Deployment complexity must not slow development
- **Testing**: Easy for testers to try multiple versions
- **Framework**: .NET 6+ with WPF

### Assumptions

- Users have Windows 10 (1809+) or Windows 11
- ~60-80 MB download size is acceptable for MVP
- Users can extract ZIP files (or we provide unzipped folder)
- No automatic updates needed for MVP (manual download acceptable)
- Application can function without Windows Registry modifications
- %APPDATA% folder is available for storing settings

---

## Decision

We will build the application as a **self-contained portable executable** using .NET's self-contained deployment model. The application will be distributed as a ZIP file containing all dependencies, requiring no installation or prerequisites.

### Core Design

**Deployment Model**:
- **Self-Contained**: Include .NET runtime in application folder
- **Single Folder**: All files in one directory (not single-file executable)
- **Portable**: Can run from any location (USB, network share, desktop)
- **No Installation**: Extract and run (xcopy deployment)
- **Settings Storage**: %APPDATA%\Proximity\ for user preferences

**Build Configuration**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>false</PublishSingleFile>
    <PublishTrimmed>false</PublishTrimmed>
    <PublishReadyToRun>true</PublishReadyToRun>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>
</Project>
```

**Publish Command**:
```bash
dotnet publish src/Proximity/Proximity.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -p:PublishTrimmed=false `
  -p:PublishReadyToRun=true `
  -o ./publish/win-x64
```

**Distribution Package Structure**:
```
Proximity-v1.0.0-win-x64.zip
├── Proximity.exe                    # Main executable
├── Proximity.dll                    # Application assembly
├── Proximity.Core.dll               # Core infrastructure
├── *.dll                            # .NET runtime and dependencies
├── modules/                         # Feature modules
│   ├── Proximity.Module.*.dll
├── appsettings.json                 # Default configuration
├── logs/                            # Log files directory (empty initially)
├── README.txt                       # Quick start guide
└── LICENSE.txt                      # License file
```

**User Settings Location**:
```
%APPDATA%\Proximity\
├── settings.json                    # User preferences
└── cache/                           # Temporary cache (if needed)
```

### Implementation Details

**Startup Behavior**:
```csharp
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Set up application data folder
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Proximity");
        
        // Create if doesn't exist (no admin rights needed)
        Directory.CreateDirectory(appDataPath);
        
        // Configure logging to application folder (not Program Files)
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logPath);
        
        // Continue normal startup...
        base.OnStartup(e);
    }
}
```

**File Paths Strategy**:
```csharp
public static class AppPaths
{
    // Application folder (where EXE is located)
    public static string ApplicationFolder => AppContext.BaseDirectory;
    
    // User settings folder (in %APPDATA%)
    public static string UserDataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Proximity");
    
    // Logs folder (in application folder)
    public static string LogsFolder => Path.Combine(ApplicationFolder, "logs");
    
    // Modules folder (in application folder)
    public static string ModulesFolder => Path.Combine(ApplicationFolder, "modules");
    
    // User settings file
    public static string SettingsFile => Path.Combine(UserDataFolder, "settings.json");
}
```

**Distribution Process**:
1. Build self-contained deployment with `dotnet publish`
2. Copy additional resources (README, LICENSE)
3. Create ZIP file with version in name
4. Upload to GitHub Releases or file server
5. Users download, extract, and run `Proximity.exe`

---

## Alternatives Considered

### Option 1: ClickOnce Deployment

**Description**: Microsoft's deployment technology that installs applications from web server with automatic updates.

**Pros**:
- Automatic updates built-in
- Simple installation from URL
- Certificate signing supported
- Rollback capability
- Partial trust execution

**Cons**:
- Still requires installation (not truly portable)
- Writes to user profile and Start menu
- More complex deployment pipeline
- Not well-suited for .NET 6+ (better for .NET Framework)
- Certificate signing adds complexity and cost
- Less common in modern .NET

**Reason for rejection**: ClickOnce still requires installation and isn't ideal for .NET 6+. Portable executable is simpler for MVP and doesn't require update infrastructure.

---

### Option 2: MSIX Package

**Description**: Modern Windows app package format with Microsoft Store integration.

**Pros**:
- Clean installation and uninstallation
- Automatic updates via Store
- Sandboxed execution (security)
- Modern Windows integration
- Certificate signing enforced

**Cons**:
- Requires Windows 10 1809+ (acceptable but limiting)
- Complex packaging process
- Requires certificate signing (cost and complexity)
- Not truly portable (must be installed)
- Sandbox limitations (file system access restrictions)
- Steep learning curve

**Reason for rejection**: MSIX is overkill for MVP and conflicts with portability goal. Packaging complexity would slow development.

---

### Option 3: Traditional MSI Installer

**Description**: Classic Windows Installer package with wizard-based installation.

**Pros**:
- Familiar to Windows users
- Professional appearance
- Can install prerequisites
- Registry integration
- Add/Remove Programs integration
- Per-machine installation option

**Cons**:
- Requires administrative privileges
- Complex to create and maintain (WiX toolset)
- Installation wizard is friction for users
- Cannot run from USB or network share
- Difficult to test (must install to test)
- Uninstall required to remove

**Reason for rejection**: Traditional installers are the opposite of our portability and low-friction goals. Too much overhead for MVP.

---

### Option 4: Single File Executable

**Description**: Use .NET's PublishSingleFile to create single EXE with all dependencies embedded.

**Pros**:
- Single file distribution (simplest)
- Self-contained (no prerequisites)
- Very portable
- Easy to distribute

**Cons**:
- Module discovery doesn't work well (modules must be separate)
- Larger file size (compressed)
- Slower first launch (extraction to temp folder)
- Cannot easily inspect dependencies
- Debugging more difficult
- **Conflicts with module system** (modules must be separate DLLs)

**Reason for rejection**: Single file executable conflicts with our modular plugin architecture. Modules must be separate assemblies for discovery to work.

---

### Option 5: Framework-Dependent Deployment

**Description**: Require users to install .NET runtime separately, deploy only application DLLs.

**Pros**:
- Smaller download size (~10 MB vs. 60-80 MB)
- Faster build and publish
- Shared runtime across multiple apps
- Easier to update runtime separately

**Cons**:
- **Requires prerequisite installation** (.NET runtime)
- Users must have correct version installed
- Poor user experience (prerequisite check, download, install)
- Conflicts with portability goal
- Deployment friction for MVP testing

**Reason for rejection**: Requiring .NET runtime installation conflicts with our zero-friction goal. Self-contained deployment is worth the size tradeoff.

---

## Consequences

### Positive Consequences

- **Zero Friction Installation**: Users extract and run, no wizard or admin rights
  - Impact: Lower barrier to entry; easier to distribute for testing; faster user adoption
  
- **True Portability**: Can run from USB drive, network share, or any folder
  - Impact: Flexible deployment options; easy to try without commitment; no system modifications
  
- **Rapid Distribution**: ZIP file can be uploaded and distributed immediately
  - Impact: Faster development cycle; quick hotfix distribution; easier beta testing
  
- **Easy Testing**: Testers can run multiple versions side-by-side
  - Impact: Easier to compare versions; safer testing; no conflicts between versions
  
- **Simple Rollback**: Delete new folder, extract old version
  - Impact: Zero-risk updates; instant rollback if issues found; confident deployments
  
- **No Prerequisites**: Works on any Windows 10/11 machine without .NET runtime
  - Impact: Broader compatibility; fewer support issues; consistent behavior
  
- **Clean Uninstall**: Delete folder and %APPDATA%\Proximity
  - Impact: No Registry cleanup needed; no leftover files; user control

### Negative Consequences

- **Large Download Size**: ~60-80 MB (includes .NET runtime)
  - **Mitigation**:
    - Acceptable for modern internet speeds (< 30 seconds on typical connection)
    - Could compress with 7-Zip for ~40-50 MB (accept tradeoff)
    - Consider download manager for resumable downloads
    - Size is one-time cost; updates can be differential (future)
  
- **No Automatic Updates**: Users must manually download new versions
  - **Mitigation**:
    - Acceptable for MVP (limited users)
    - Add update notification (check for new version, link to download)
    - Post-MVP: Implement auto-update mechanism
    - Document update process clearly
  
- **Multiple Versions**: Users might run outdated versions unknowingly
  - **Mitigation**:
    - Display version prominently in UI
    - Add "check for updates" feature
    - Notify in-app when new version available
    - Clear versioning in file names (Proximity-v1.2.3.zip)
  
- **First Launch Detection**: Cannot distinguish first run from reinstall easily
  - **Mitigation**:
    - Check for %APPDATA%\Proximity\settings.json existence
    - Show welcome screen if settings don't exist
    - Acceptable limitation for MVP
  
- **No Code Signing**: Unsigned executable may trigger SmartScreen warnings
  - **Mitigation**:
    - Document SmartScreen warning in README (expected for unsigned apps)
    - Provide screenshot of "More info" → "Run anyway" process
    - Post-MVP: Get code signing certificate if budget allows
    - Build reputation over time (SmartScreen learns)

### Neutral Consequences

- **Storage Duplication**: Each user has their own .NET runtime copy
  - Neither clearly positive nor negative: Small disk space cost for simplicity

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation Strategy |
|------|-------------|--------|---------------------|
| SmartScreen blocks downloads | High | Medium | Document in README; guide users through override; build reputation |
| Users confused by ZIP extraction | Medium | Low | Provide extracted folder download option; clear README instructions |
| File antivirus false positives | Low | Medium | Submit to antivirus vendors; build reputation; sign code post-MVP |
| Large download size deters users | Low | Low | Compress with 7-Zip; provide size warning; acceptable for target users |
| Users run old versions | Medium | Medium | Check for updates feature; version display in UI; release notes |
| %APPDATA% permissions issues | Low | High | Graceful fallback to application folder; clear error message |

---

## Implementation

### Action Items

- [x] Configure project for self-contained deployment
- [x] Set up publish profile with correct settings
- [ ] Create application paths helper (AppPaths class)
- [ ] Test extraction and execution on clean Windows 10/11 machines
- [ ] Create README.txt with quick start instructions
- [ ] Set up build script for automated packaging
- [ ] Test portability (USB drive, network share)
- [ ] Document deployment process
- [ ] Create version display in UI
- [ ] Test SmartScreen behavior (unsigned app)

### Success Criteria

**Functional**:
- ✅ Application runs without installing .NET runtime
- ✅ Can run from any folder location
- ✅ Settings persist in %APPDATA%\Proximity\
- ✅ Multiple versions can coexist
- ✅ Logs written to application folder (no permission errors)
- ✅ No admin rights required

**User Experience**:
- ✅ Extract and run (no wizard)
- ✅ < 2 minutes from download to running
- ✅ Clear instructions in README
- ✅ Version visible in UI

**Technical**:
- ✅ Published folder size < 100 MB
- ✅ Startup time < 3 seconds (cold start)
- ✅ Works on Windows 10 (1809+) and Windows 11
- ✅ No runtime errors on clean machine

### Timeline

- **Decision Date**: 2024-01-21
- **Implementation Start**: 2024-01-22
- **Expected Completion**: 2024-01-26 (with first build)
- **Review Date**: 2024-02-09 (after user testing feedback)

---

## Related Decisions

- **ADR-001**: Use Modular Plugin-Style Architecture - Modules as separate DLLs (not single file)
- **ADR-002**: Use Dependency Injection - DI works seamlessly with self-contained deployment
- **ADR-003**: Use WPF with MVVM Pattern - WPF apps work well with self-contained deployment
- **Future ADR**: Auto-Update Strategy - May add post-MVP without changing portable model

---

## References

- [.NET Self-Contained Deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/deploy-with-cli#publish-self-contained)
- [Publish Single File](https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file)
- [ReadyToRun Compilation](https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run)
- [ClickOnce Deployment](https://docs.microsoft.com/en-us/visualstudio/deployment/clickonce-security-and-deployment)
- [MSIX Packaging](https://docs.microsoft.com/en-us/windows/msix/overview)
- [Code Signing for Windows](https://docs.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools)

---

## Notes

### README.txt Content

```
Proximity MVP - Windows Desktop Application

Quick Start:
1. Extract all files to a folder (e.g., C:\Apps\Proximity)
2. Run Proximity.exe
3. That's it! No installation required.

Notes:
- First launch may show Windows SmartScreen warning (unsigned app)
- Click "More info" then "Run anyway" to proceed
- Settings stored in: %APPDATA%\Proximity\
- Logs stored in: logs/ folder (for troubleshooting)

To Update:
1. Download new version
2. Extract to new folder (or replace old files)
3. Run Proximity.exe
4. Your settings are preserved

To Uninstall:
1. Delete the Proximity folder
2. Delete %APPDATA%\Proximity\ (optional, to remove settings)

Requirements:
- Windows 10 (version 1809 or later) or Windows 11
- No additional software needed

Support:
- Report issues: [GitHub Issues URL]
- Documentation: [Documentation URL]
```

### Build Script Example

```powershell
# Build script: build-release.ps1
$version = "1.0.0"
$outputDir = ".\publish\win-x64"
$packageName = "Proximity-v$version-win-x64"

# Clean previous build
Remove-Item -Path $outputDir -Recurse -ErrorAction SilentlyContinue

# Publish
dotnet publish src/Proximity/Proximity.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o $outputDir

# Copy additional files
Copy-Item "README.txt" -Destination $outputDir
Copy-Item "LICENSE.txt" -Destination $outputDir

# Create modules directory
New-Item -Path "$outputDir\modules" -ItemType Directory -Force

# Create logs directory (empty)
New-Item -Path "$outputDir\logs" -ItemType Directory -Force

# Create ZIP
Compress-Archive -Path "$outputDir\*" -DestinationPath ".\$packageName.zip" -Force

Write-Host "Build complete: $packageName.zip"
Write-Host "Size: $((Get-Item ".\$packageName.zip").Length / 1MB) MB"
```

### Version Display

```csharp
// Display version in UI
public class MainViewModel : ObservableObject
{
    public string Version => 
        $"v{Assembly.GetExecutingAssembly().GetName().Version}";
    
    public string Title => $"Proximity {Version}";
}
```

---

## Updates

### 2024-01-21
Initial ADR created based on architecture design for Issue #12.
