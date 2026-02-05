# ADR-002: Use Dependency Injection for Loose Coupling

**Status**: Proposed

**Date**: 2024-01-21

**Decision Makers**: Architecture Team, Development Team

**Tags**: architecture, dependency-injection, testability, mvp

---

## Context

### Problem Statement

We need an approach to manage dependencies between components that:
- Enables easy unit testing through dependency mocking
- Makes dependencies explicit and visible
- Supports changing implementations without modifying consumers
- Follows SOLID principles (especially Dependency Inversion)
- Integrates well with the modular architecture
- Manages object lifecycles consistently

Traditional approaches like service locator pattern or static dependencies create tight coupling, make testing difficult, and hide dependencies. Manual dependency management (new() everywhere) is error-prone and makes refactoring painful.

### Background

The Proximity MVP uses a modular architecture where features are developed independently. Components need to interact with services and each other while maintaining loose coupling. The application must be testable, as we plan to achieve >80% test coverage for core infrastructure.

Modern .NET applications standardize on constructor injection with DI containers. The `Microsoft.Extensions.DependencyInjection` package is the de facto standard for .NET applications, used by ASP.NET Core, MAUI, and other .NET frameworks.

### Constraints

- **Technology**: .NET 6+ provides built-in DI container
- **Team Knowledge**: Team has varying experience with DI patterns
- **Testability**: Must support easy unit testing with mocking
- **Performance**: DI container overhead must be negligible for desktop app
- **MVVM Pattern**: Must integrate well with WPF and MVVM pattern

### Assumptions

- Most services can use constructor injection (not property or method injection)
- Service lifetimes are straightforward (singleton, transient, scoped)
- Performance overhead of DI container is acceptable for desktop app
- Team can learn DI patterns with proper documentation

---

## Decision

We will use **Microsoft.Extensions.DependencyInjection** throughout the application with **constructor injection** as the primary dependency injection pattern.

### Core Design

**Dependency Injection Container**:
- Use `Microsoft.Extensions.DependencyInjection` as the IoC container
- Configure container at application startup in `App.xaml.cs`
- Modules register their services during initialization
- ViewModels and services use constructor injection exclusively

**Service Lifetimes**:
```csharp
// Singleton: One instance for application lifetime
services.AddSingleton<IModuleManager, ModuleManager>();
services.AddSingleton<INavigationService, NavigationService>();
services.AddSingleton<IConfigurationManager, ConfigurationManager>();

// Transient: New instance each time resolved
services.AddTransient<MainViewModel>();
services.AddTransient<IFeatureService, FeatureService>();

// Scoped: Not typically used in desktop apps (web concept)
// Avoided in our architecture
```

**Constructor Injection Pattern**:
```csharp
public class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IModuleManager _moduleManager;
    private readonly ILogger<MainViewModel> _logger;
    
    // Dependencies injected via constructor
    public MainViewModel(
        INavigationService navigationService,
        IModuleManager moduleManager,
        ILogger<MainViewModel> logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public void NavigateToFeature()
    {
        _navigationService.NavigateToAsync<FeatureViewModel>();
    }
}
```

### Implementation Details

**Application Startup**:
```csharp
public partial class App : Application
{
    private IServiceProvider _serviceProvider;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        
        // Resolve and show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Register infrastructure services
        services.AddSingleton<ILogger>(s => CreateLogger());
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IModuleManager, ModuleManager>();
        
        // Register application components
        services.AddSingleton<MainWindow>();
        services.AddTransient<MainViewModel>();
        
        // Discover and load modules (they register their own services)
        var moduleManager = services.BuildServiceProvider().GetRequiredService<IModuleManager>();
        moduleManager.DiscoverAndLoadModules(services);
    }
}
```

**Interface-Based Design**:
```csharp
// Define interfaces for all services
public interface INavigationService
{
    Task<bool> NavigateToAsync<TViewModel>(object parameter = null) where TViewModel : class;
    bool CanGoBack { get; }
    void GoBack();
}

// Implement interface
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    
    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    // Implementation...
}
```

**Testing with Mocks**:
```csharp
[Fact]
public void NavigateToFeature_CallsNavigationService()
{
    // Arrange - Mock dependencies
    var mockNavService = new Mock<INavigationService>();
    var mockModuleManager = new Mock<IModuleManager>();
    var mockLogger = new Mock<ILogger<MainViewModel>>();
    
    var viewModel = new MainViewModel(
        mockNavService.Object,
        mockModuleManager.Object,
        mockLogger.Object);
    
    // Act
    viewModel.NavigateToFeature();
    
    // Assert
    mockNavService.Verify(
        n => n.NavigateToAsync<FeatureViewModel>(null),
        Times.Once);
}
```

---

## Alternatives Considered

### Option 1: Service Locator Pattern

**Description**: Central registry that components query to get dependencies: `ServiceLocator.GetService<INavigationService>()`.

**Pros**:
- Simple to understand
- No constructor parameter explosion
- Easy to add new dependencies

**Cons**:
- Hidden dependencies (not visible in constructor)
- Makes testing harder (must configure service locator)
- Runtime errors instead of compile-time errors
- Anti-pattern in modern software design
- Violates Dependency Inversion Principle

**Reason for rejection**: Service Locator is considered an anti-pattern. It hides dependencies and makes code harder to test and understand. Constructor injection is explicit and compile-time safe.

---

### Option 2: Manual Dependency Management (new())

**Description**: Create dependencies directly using `new()` keyword wherever needed.

**Pros**:
- Very simple (no frameworks needed)
- Full control over object creation
- No "magic" or reflection
- Easy to understand for beginners

**Cons**:
- Tight coupling between components
- Extremely difficult to test (can't mock dependencies)
- Difficult to change implementations
- Violates SOLID principles
- Leads to duplicated initialization code
- Hard to manage object lifecycles

**Reason for rejection**: Makes testing nearly impossible and creates tight coupling. Not suitable for a maintainable, testable application.

---

### Option 3: Autofac (Third-Party DI Container)

**Description**: Use Autofac, a popular third-party DI container with advanced features.

**Pros**:
- More features than built-in DI (modules, decorators, interceptors)
- Better assembly scanning
- More flexible lifetime management
- Mature and well-tested

**Cons**:
- External dependency (not built-in)
- More complex API
- Heavier than needed for MVP
- Team must learn Autofac-specific patterns
- Less common in modern .NET (built-in is standard)

**Reason for rejection**: Built-in `Microsoft.Extensions.DependencyInjection` is sufficient for our needs and is the .NET standard. Autofac's advanced features aren't needed for MVP.

---

### Option 4: Property Injection

**Description**: Dependencies injected via properties instead of constructor.

**Pros**:
- Cleaner constructors (no parameters)
- Optional dependencies are easy
- Can set dependencies after construction

**Cons**:
- Dependencies not guaranteed at construction time
- Easy to forget to set a dependency
- Harder to see what's required
- Testing more complex (must set properties)
- Not immutable

**Reason for rejection**: Constructor injection is more explicit and ensures dependencies are available when object is created. Property injection should only be used for truly optional dependencies (rare in our architecture).

---

## Consequences

### Positive Consequences

- **Testability**: Easy to unit test by mocking dependencies
  - Impact: Achieves >80% test coverage target; faster test development; more reliable tests
  
- **Explicit Dependencies**: Constructor signature shows all dependencies clearly
  - Impact: Easier to understand component requirements; better code reviews; prevents hidden dependencies
  
- **Loose Coupling**: Components depend on interfaces, not implementations
  - Impact: Can change implementations without affecting consumers; supports modular architecture
  
- **Lifecycle Management**: DI container manages object creation and disposal
  - Impact: No memory leaks from forgetting to dispose; consistent lifecycle patterns
  
- **SOLID Compliance**: Supports Dependency Inversion Principle
  - Impact: More maintainable code; easier refactoring; cleaner architecture
  
- **Consistency**: Same pattern used throughout application
  - Impact: Easier onboarding; predictable code structure; less cognitive load

### Negative Consequences

- **Learning Curve**: Developers unfamiliar with DI must learn the pattern
  - **Mitigation**:
    - Comprehensive documentation with examples
    - Code templates for common patterns
    - Pair programming for first implementations
    - Team training session on DI basics
    - Clear error messages when DI is misconfigured
  
- **Constructor Complexity**: Classes with many dependencies have large constructors
  - **Mitigation**:
    - Limit dependencies to 3-5 per class (refactor if more)
    - Use interface aggregation (combine related dependencies)
    - Review architecture if classes need many dependencies
    - Consider if class has too many responsibilities (SRP violation)
  
- **Performance Overhead**: DI resolution has small performance cost
  - **Mitigation**:
    - Use singletons for frequently-resolved services
    - DI overhead is ~microseconds (negligible for desktop app)
    - Performance testing validates acceptable overhead
    - Acceptable tradeoff for maintainability gains
  
- **Circular Dependencies**: Possible to create circular dependency chains
  - **Mitigation**:
    - Design guidelines prohibit circular dependencies
    - DI container detects and throws clear error
    - Architecture reviews catch potential cycles
    - Use events or mediator pattern to break cycles
  
- **Runtime Errors**: Missing registrations discovered at runtime, not compile-time
  - **Mitigation**:
    - Validate DI configuration at startup (call `ValidateOnBuild()`)
    - Integration tests cover critical dependency chains
    - Clear error messages identify missing registrations
    - Fail-fast at startup (don't defer to user interaction)

### Neutral Consequences

- **Framework Dependency**: Application depends on `Microsoft.Extensions.DependencyInjection`
  - This is acceptable as it's a core .NET library with broad ecosystem support

---

## Risks & Mitigations

| Risk | Probability | Impact | Mitigation Strategy |
|------|-------------|--------|---------------------|
| Developers create circular dependencies | Medium | High | Design reviews; DI container detection; clear error messages; architecture guidelines |
| Missing service registrations cause runtime crashes | Medium | High | Startup validation; integration tests; fail-fast initialization; comprehensive logging |
| Constructor parameter explosion (too many dependencies) | Medium | Medium | Limit to 3-5 dependencies; refactor if more; code reviews enforce limit |
| Team unfamiliar with DI patterns | High | Medium | Training session; documentation with examples; templates; pair programming |
| Incorrect lifetime management causes bugs | Low | High | Document lifetime patterns; code reviews; common services as singletons |
| Performance issues from DI overhead | Low | Low | Performance testing; use singletons appropriately; acceptable overhead |

---

## Implementation

### Action Items

- [x] Add `Microsoft.Extensions.DependencyInjection` NuGet package
- [x] Configure DI container in `App.xaml.cs`
- [ ] Create service registration documentation
- [ ] Define lifetime guidelines (singleton vs. transient)
- [ ] Implement startup validation
- [ ] Create ViewModels with constructor injection
- [ ] Add unit test examples with mocking
- [ ] Document patterns for common scenarios
- [ ] Add integration tests for DI configuration
- [ ] Create code templates for services and ViewModels

### Success Criteria

**Functional**:
- ✅ All services registered with appropriate lifetimes
- ✅ ViewModels resolved via DI (not manually instantiated)
- ✅ No service locator pattern used anywhere
- ✅ All dependencies injected via constructors
- ✅ DI configuration validated at startup

**Testing**:
- ✅ ViewModels can be unit tested with mocked dependencies
- ✅ >80% test coverage achieved for core services
- ✅ Integration tests validate DI configuration

**Code Quality**:
- ✅ No circular dependencies
- ✅ Constructors have ≤5 parameters
- ✅ All interfaces registered in DI container

### Timeline

- **Decision Date**: 2024-01-21
- **Implementation Start**: 2024-01-22
- **Expected Completion**: 2024-01-26
- **Review Date**: 2024-02-15 (after first features implemented, assess effectiveness)

---

## Related Decisions

- **ADR-001**: Use Modular Plugin-Style Architecture - Modules register services via DI
- **ADR-003**: Use WPF with MVVM Pattern - ViewModels resolved via DI container
- **Future ADR**: Error Handling Strategy - DI simplifies error handler registration

---

## References

- [Dependency Injection in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection)
- [Dependency Injection Principles, Practices, and Patterns (Book)](https://www.manning.com/books/dependency-injection-principles-practices-patterns)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Service Locator is an Anti-Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/)

---

## Notes

### Service Lifetime Guidelines

**Singleton** (`AddSingleton`):
- Use for: Stateless services, expensive-to-create objects, truly shared state
- Examples: Configuration, logging, module manager, navigation service
- Thread-safety: Must be thread-safe if used across threads

**Transient** (`AddTransient`):
- Use for: ViewModels, services with per-call state, lightweight objects
- Examples: ViewModels, lightweight services
- Performance: No overhead concern for desktop app

**Scoped** (`AddScoped`):
- Use for: Rarely in desktop apps (web concept for per-request lifetime)
- Examples: Database context in web apps (not applicable here)
- Note: Avoid in desktop apps to prevent confusion

### Common Patterns

**Injecting IServiceProvider**:
```csharp
// Only when you need to resolve dependencies dynamically
public class Factory
{
    private readonly IServiceProvider _serviceProvider;
    
    public Factory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IFeature CreateFeature(string type)
    {
        // Dynamic resolution based on runtime information
        return _serviceProvider.GetRequiredService(GetFeatureType(type));
    }
}
```

**Optional Dependencies**:
```csharp
// Use nullable reference types
public class Service
{
    private readonly IOptionalDependency? _optional;
    
    public Service(IOptionalDependency? optional = null)
    {
        _optional = optional;
    }
}
```

### Anti-Patterns to Avoid

❌ **Service Locator**:
```csharp
// DON'T DO THIS
public class ViewModel
{
    public void DoSomething()
    {
        var service = ServiceLocator.GetService<IMyService>();
        service.Execute();
    }
}
```

✅ **Constructor Injection**:
```csharp
// DO THIS INSTEAD
public class ViewModel
{
    private readonly IMyService _service;
    
    public ViewModel(IMyService service)
    {
        _service = service;
    }
    
    public void DoSomething()
    {
        _service.Execute();
    }
}
```

---

## Updates

### 2024-01-21
Initial ADR created based on architecture design for Issue #12.
