# ADR-003: Use WPF with MVVM Pattern

**Status**: Proposed

**Date**: 2024-01-21

**Decision Makers**: Architecture Team, Development Team

**Tags**: architecture, ui, wpf, mvvm, presentation

---

## Context

### Problem Statement

We need a UI framework and architectural pattern for building a Windows desktop application that:
- Separates UI concerns from business logic for testability
- Enables rapid UI development with designer support
- Provides rich, responsive user interfaces with data binding
- Supports the modular architecture (features as independent modules)
- Has mature tooling and ecosystem support
- Allows parallel development of UI and logic

Traditional approaches with code-behind create tight coupling between UI and logic, making testing difficult and changes expensive. We need a pattern that enforces separation of concerns while remaining productive for MVP development.

### Background

The Proximity MVP is a Windows desktop application requiring a modern, responsive UI. The development team needs to deliver features quickly while maintaining code quality and testability. The application will use a modular architecture where features can be developed independently.

Modern .NET desktop applications typically use either WPF or WinUI 3, with MVVM being the standard architectural pattern for both. The team has some WPF experience but limited experience with MVVM at scale.

### Constraints

- **Platform**: Windows 10/11 desktop only (no cross-platform requirement for MVP)
- **Technology**: .NET 6+ framework
- **Timeline**: Aggressive MVP timeline requires productive framework
- **Team Skills**: Team has basic WPF knowledge, needs to learn MVVM
- **Testability**: Must support >80% test coverage for business logic
- **Modularity**: Must integrate with plugin-based architecture

### Assumptions

- Windows-only deployment is acceptable for MVP
- Rich desktop UI is preferred over web-based UI
- Team can learn MVVM pattern with proper support
- WPF stability is more important than bleeding-edge features
- Designer support accelerates UI development

---

## Decision

We will use **WPF (Windows Presentation Foundation)** as the UI framework with **MVVM (Model-View-ViewModel)** architectural pattern, leveraging **CommunityToolkit.Mvvm** to reduce boilerplate code.

### Core Design

**Framework Stack**:
- **UI Framework**: WPF with XAML
- **MVVM Library**: CommunityToolkit.Mvvm (formerly MVVM Toolkit)
- **Data Binding**: WPF's built-in data binding engine
- **Commands**: ICommand via CommunityToolkit.Mvvm's RelayCommand
- **Property Notification**: INotifyPropertyChanged via ObservableObject base class

**MVVM Separation**:
```
┌─────────────────────────────────────────────────┐
│ View (XAML)                                     │
│ - UI Elements (Buttons, TextBoxes, etc.)       │
│ - Layout and Styling                            │
│ - Data Binding Expressions                      │
│ - No Business Logic                             │
└─────────────────────────────────────────────────┘
              ↕ (Data Binding)
┌─────────────────────────────────────────────────┐
│ ViewModel (C#)                                  │
│ - Properties (Data for View)                    │
│ - Commands (User Actions)                       │
│ - View Logic                                    │
│ - No Direct UI References                       │
└─────────────────────────────────────────────────┘
              ↓ (Method Calls)
┌─────────────────────────────────────────────────┐
│ Model / Services (C#)                           │
│ - Business Logic                                │
│ - Data Access                                   │
│ - Validation                                    │
└─────────────────────────────────────────────────┘
```

### Implementation Details

**ViewModel Example (using CommunityToolkit.Mvvm)**:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly ILogger<MainViewModel> _logger;
    
    // Constructor injection from DI container
    public MainViewModel(
        INavigationService navigationService,
        ILogger<MainViewModel> logger)
    {
        _navigationService = navigationService;
        _logger = logger;
    }
    
    // Observable property with automatic notification
    [ObservableProperty]
    private string _title = "Proximity MVP";
    
    [ObservableProperty]
    private bool _isLoading;
    
    // Collection with automatic notification
    [ObservableProperty]
    private ObservableCollection<string> _items = new();
    
    // Command that can be bound to buttons
    [RelayCommand]
    private async Task NavigateToSettings()
    {
        _logger.LogInformation("Navigating to settings");
        await _navigationService.NavigateToAsync<SettingsViewModel>();
    }
    
    // Command with can-execute logic
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        IsLoading = true;
        try
        {
            // Save logic
            _logger.LogInformation("Saved successfully");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private bool CanSave() => !IsLoading && Items.Count > 0;
}
```

**View Example (XAML)**:
```xml
<Window x:Class="Proximity.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:Proximity.ViewModels"
        Title="{Binding Title}"
        Width="1200" Height="800">
    
    <Window.DataContext>
        <!-- ViewModel resolved via DI, set in code-behind -->
    </Window.DataContext>
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <Button Content="Settings" 
                    Command="{Binding NavigateToSettingsCommand}"
                    Margin="5"/>
            <Button Content="Save" 
                    Command="{Binding SaveCommand}"
                    Margin="5"/>
        </StackPanel>
        
        <!-- Content -->
        <ScrollViewer Grid.Row="1">
            <ItemsControl ItemsSource="{Binding Items}">
                <!-- Item template -->
            </ItemsControl>
        </ScrollViewer>
        
        <!-- Loading indicator -->
        <Grid Grid.RowSpan="2" 
              Background="#80000000" 
              Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}">
            <ProgressBar IsIndeterminate="True" Width="200" Height="20"/>
        </Grid>
    </Grid>
</Window>
```

**Code-Behind (Minimal)**:
```csharp
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel; // ViewModel injected via DI
    }
}
```

**View Registration with Navigation**:
```csharp
// In module initialization
public void Initialize(IServiceCollection services)
{
    // Register ViewModel
    services.AddTransient<SettingsViewModel>();
    
    // Register View
    services.AddTransient<SettingsView>();
    
    // Register mapping for navigation
    services.RegisterView<SettingsViewModel, SettingsView>();
}
```

---

## Alternatives Considered

### Option 1: WinUI 3

**Description**: Microsoft's modern UI framework for Windows, successor to UWP.

**Pros**:
- Modern design language (Fluent Design)
- Better performance than WPF
- Active development by Microsoft
- Touch and pen input support
- Better high-DPI support

**Cons**:
- Less mature than WPF (fewer third-party controls)
- Deployment complexity (requires Windows App SDK)
- Smaller community and fewer resources
- Some stability concerns (newer framework)
- Limited designer support in Visual Studio
- Packaging requirements complicate portable executable goal

**Reason for rejection**: WinUI 3 is not mature enough for MVP. Deployment complexity conflicts with portable executable requirement. WPF's maturity and ecosystem are more valuable for rapid MVP development.

---

### Option 2: Avalonia UI

**Description**: Cross-platform .NET UI framework with WPF-like XAML syntax.

**Pros**:
- Cross-platform (Windows, macOS, Linux)
- Similar to WPF (familiar XAML)
- Modern architecture
- Growing community

**Cons**:
- Smaller ecosystem than WPF
- Fewer third-party controls
- Less designer support
- Not as mature as WPF
- Learning curve for platform differences
- Overkill for Windows-only MVP

**Reason for rejection**: Cross-platform capability not needed for MVP. WPF's maturity and tooling support are more valuable than cross-platform potential.

---

### Option 3: Windows Forms

**Description**: Legacy Windows UI framework, event-driven model.

**Pros**:
- Very simple and straightforward
- Rapid prototyping
- Excellent designer support
- Familiar to many developers
- Small learning curve

**Cons**:
- Legacy technology (limited future)
- Poor data binding support
- Difficult to achieve clean MVVM separation
- Limited styling and theming capabilities
- Less modern look and feel
- Hard to test (tight coupling to UI)

**Reason for rejection**: Windows Forms lacks the data binding and architectural patterns needed for maintainable, testable code. WPF is the modern standard for Windows desktop applications.

---

### Option 4: Electron (Web-Based)

**Description**: Build desktop app using web technologies (HTML, CSS, JavaScript).

**Pros**:
- Cross-platform
- Leverage web development skills
- Rich ecosystem of web libraries
- Easy UI prototyping

**Cons**:
- Large bundle size (100+ MB)
- High memory usage
- Slower startup time
- Non-native look and feel
- Not optimal for Windows-specific features
- Team would need to learn web stack

**Reason for rejection**: Electron is overkill for Windows-only MVP. Large bundle size conflicts with goals. Native Windows app provides better performance and user experience.

---

### Option 5: Code-Behind Only (No MVVM)

**Description**: Use WPF but put logic directly in code-behind files instead of ViewModels.

**Pros**:
- Simpler for small applications
- No MVVM learning curve
- Direct access to UI controls
- Faster initial development

**Cons**:
- Tight coupling between UI and logic
- Very difficult to unit test
- Hard to refactor UI without breaking logic
- Doesn't support modular architecture well
- Code duplication across views
- Poor separation of concerns

**Reason for rejection**: Code-behind approach doesn't support testability goals (>80% coverage). MVVM is industry standard for maintainable WPF applications.

---

## Consequences

### Positive Consequences

- **Testability**: ViewModels can be unit tested without UI
  - Impact: Achieves >80% test coverage target; no UI automation needed for business logic tests; faster test execution
  
- **Separation of Concerns**: Clear boundary between UI and business logic
  - Impact: UI changes don't break logic; logic changes don't require UI changes; parallel development possible
  
- **Data Binding**: Automatic UI updates when ViewModel properties change
  - Impact: Less boilerplate code; fewer bugs from manual UI updates; more responsive UI
  
- **Designer Support**: Visual Studio XAML designer accelerates UI development
  - Impact: Faster prototyping; easier layout adjustments; live preview
  
- **Mature Ecosystem**: Rich set of third-party controls and libraries
  - Impact: Don't need to build everything from scratch; proven solutions available
  
- **Command Pattern**: Clean handling of user actions via ICommand
  - Impact: Enable/disable logic separate from action logic; easy to test; reusable commands
  
- **CommunityToolkit.Mvvm**: Source generators reduce boilerplate
  - Impact: Faster ViewModel development; less repetitive code; fewer bugs from manual INotifyPropertyChanged

### Negative Consequences

- **MVVM Learning Curve**: Team must learn MVVM pattern and data binding
  - **Mitigation**:
    - Comprehensive training session on MVVM fundamentals
    - Provide ViewModel templates and examples
    - Code reviews to enforce patterns
    - Pair programming for first features
    - Documentation with common scenarios
    - CommunityToolkit.Mvvm simplifies implementation
  
- **Data Binding Complexity**: Advanced binding scenarios can be tricky
  - **Mitigation**:
    - Document common binding patterns (converters, multi-binding, etc.)
    - Create reusable value converters
    - Use code for complex scenarios (not everything needs binding)
    - Provide troubleshooting guide for binding errors
  
- **XAML Verbosity**: XAML can be verbose for complex UIs
  - **Mitigation**:
    - Use styles and templates to reduce repetition
    - Create reusable user controls
    - Leverage Resource Dictionaries for shared styles
    - Accept verbosity as tradeoff for designer support
  
- **View-ViewModel Coupling**: Still some coupling through binding paths (strings)
  - **Mitigation**:
    - Use strong typing where possible (x:Bind in UWP, but not available in WPF)
    - Compile-time checking via naming conventions
    - Good test coverage catches binding errors early
    - ReSharper/Rider provide XAML binding analysis
  
- **Performance**: Data binding has slight overhead
  - **Mitigation**:
    - WPF's binding is highly optimized
    - Use virtualization for large lists
    - Minimize property notifications
    - Performance overhead negligible for desktop app

### Neutral Consequences

- **XAML vs. Code**: Some developers prefer UI in code rather than XAML
  - Team preference may vary; XAML is standard for WPF

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation Strategy |
|------|-------------|--------|---------------------|
| Team struggles with MVVM concepts | Medium | Medium | Training session; templates; pair programming; comprehensive docs |
| Data binding errors hard to debug | Medium | Medium | Enable detailed binding error logging; use binding converters for troubleshooting; document common issues |
| Complex UI requirements exceed WPF capabilities | Low | High | Evaluate requirements during design; use third-party controls if needed; acceptable for MVP |
| ViewModel becomes too complex | Medium | Medium | Enforce single responsibility; limit ViewModels to view logic only; extract services for business logic |
| Memory leaks from event handlers | Low | High | Use weak event pattern; properly dispose ViewModels; memory profiling during development |
| Performance issues with large data sets | Low | Medium | Use virtualization (VirtualizingStackPanel); lazy loading; pagination where appropriate |

---

## Implementation

### Action Items

- [x] Add WPF project to solution (net6.0-windows target framework)
- [x] Add CommunityToolkit.Mvvm NuGet package
- [ ] Create base ViewModel class/template
- [ ] Create sample View and ViewModel demonstrating patterns
- [ ] Document MVVM guidelines and best practices
- [ ] Create reusable value converters (BoolToVisibility, etc.)
- [ ] Set up navigation infrastructure with View-ViewModel mapping
- [ ] Create UI style guide with theme resources
- [ ] Add unit test examples for ViewModels
- [ ] Conduct team training on MVVM pattern

### Success Criteria

**Architecture**:
- ✅ All Views have corresponding ViewModels
- ✅ No business logic in code-behind (only UI initialization)
- ✅ ViewModels have no direct UI references
- ✅ All user actions handled via ICommand
- ✅ All displayed data via property binding

**Testing**:
- ✅ ViewModels unit tested without UI
- ✅ >80% coverage of ViewModel logic
- ✅ Mocked dependencies in tests

**Code Quality**:
- ✅ Consistent naming conventions (ViewModel suffix, etc.)
- ✅ Proper use of ObservableObject and property notification
- ✅ Commands follow CanExecute pattern where applicable
- ✅ No memory leaks from event subscriptions

### Timeline

- **Decision Date**: 2024-01-21
- **Implementation Start**: 2024-01-22
- **Expected Completion**: 2024-02-09 (with WPF shell complete)
- **Review Date**: 2024-02-16 (assess pattern effectiveness after first features)

---

## Related Decisions

- **ADR-001**: Use Modular Plugin-Style Architecture - Modules contain Views and ViewModels
- **ADR-002**: Use Dependency Injection - ViewModels resolved via DI container
- **Future ADR**: Theming Strategy - Define light/dark theme implementation

---

## References

- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [MVVM Pattern](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/enterprise-application-patterns/mvvm)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [WPF Apps With The Model-View-ViewModel Design Pattern](https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern)
- [Data Binding Overview](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/)

---

## Notes

### MVVM Best Practices

**ViewModel Guidelines**:
1. **No UI References**: ViewModels should never reference UI controls or WPF types
2. **Interface-Based Dependencies**: Depend on interfaces, not concrete types
3. **Single Responsibility**: One ViewModel per View (don't reuse ViewModels across multiple Views)
4. **Testability First**: If you can't easily test it, refactor
5. **Async Commands**: Use `AsyncRelayCommand` for operations that await
6. **Property Notification**: Use `[ObservableProperty]` for automatic notification

**View Guidelines**:
1. **Minimal Code-Behind**: Only ViewModel assignment and UI-specific setup
2. **No Logic**: All logic belongs in ViewModel or Services
3. **Strong Typing**: Use `x:Name` for elements that need code-behind access
4. **Resource Management**: Use Resource Dictionaries for shared styles
5. **Data Context**: Set via DI in constructor (not XAML)

**Common Patterns**:

**Value Converters**:
```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? Visibility.Visible : Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (Visibility)value == Visibility.Visible;
    }
}
```

**Async Commands**:
```csharp
[RelayCommand]
private async Task LoadData()
{
    IsLoading = true;
    try
    {
        var data = await _dataService.GetDataAsync();
        Items = new ObservableCollection<Item>(data);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load data");
        ErrorMessage = "Failed to load data. Please try again.";
    }
    finally
    {
        IsLoading = false;
    }
}
```

**Property Validation**:
```csharp
[ObservableProperty]
[NotifyDataErrorInfo] // Enables validation
[Required]
[MinLength(3)]
[MaxLength(50)]
private string _username = string.Empty;
```

### Testing ViewModels

```csharp
public class MainViewModelTests
{
    private readonly Mock<INavigationService> _mockNavService;
    private readonly Mock<ILogger<MainViewModel>> _mockLogger;
    private readonly MainViewModel _viewModel;
    
    public MainViewModelTests()
    {
        _mockNavService = new Mock<INavigationService>();
        _mockLogger = new Mock<ILogger<MainViewModel>>();
        _viewModel = new MainViewModel(_mockNavService.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task NavigateToSettings_CallsNavigationService()
    {
        // Act
        await _viewModel.NavigateToSettingsCommand.ExecuteAsync(null);
        
        // Assert
        _mockNavService.Verify(
            n => n.NavigateToAsync<SettingsViewModel>(null),
            Times.Once);
    }
    
    [Fact]
    public void Title_PropertyChangedFired()
    {
        // Arrange
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Title))
                propertyChangedFired = true;
        };
        
        // Act
        _viewModel.Title = "New Title";
        
        // Assert
        Assert.True(propertyChangedFired);
        Assert.Equal("New Title", _viewModel.Title);
    }
}
```

---

## Updates

### 2024-01-21
Initial ADR created based on architecture design for Issue #12.
