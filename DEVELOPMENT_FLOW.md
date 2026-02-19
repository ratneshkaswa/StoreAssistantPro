# StoreAssistantPro — Development Flow

> **Rule: New Feature = New Module.**
> Follow this checklist every time. Never skip a step.

---

## Development Checklist

| # | Step | Output |
|---|---|---|
| 1 | Create module folder | `Modules/{ModuleName}/` |
| 2 | Create models | `Models/` or `Modules/{Module}/Models/` |
| 3 | Create service interface + implementation | `Modules/{Module}/Services/I{Name}Service.cs` + `{Name}Service.cs` |
| 4 | Create commands + handlers | `Modules/{Module}/Commands/{Name}Command.cs` + `{Name}Handler.cs` |
| 5 | Create ViewModel | `Modules/{Module}/ViewModels/{Name}ViewModel.cs` — inherits `BaseViewModel` |
| 6 | Create View | `Modules/{Module}/Views/{Name}View.xaml` + `.xaml.cs` |
| 7 | Register in DI | `Modules/{Module}/{Module}Module.cs` — services, handlers, ViewModels, Views |
| 8 | Add navigation / dialog entry | Register page in `NavigationPageRegistry` or dialog in `IWindowRegistry` |
| 9 | Add events if needed | `Modules/{Module}/Events/{Name}Event.cs` — publish from handler, subscribe from ViewModel |
| 10 | Add feature flag | `Core/Features/FeatureFlags.cs` + `appsettings.json` → bind visibility in XAML |
| 11 | Write tests | `Tests/Commands/`, `Tests/ViewModels/` — one test class per handler + ViewModel |

---

## Module Structure Template

```
Modules/
└── {ModuleName}/
    ├── {ModuleName}Module.cs        ← DI registration
    ├── Commands/
    │   ├── {Action}Command.cs       ← ICommand record
    │   └── {Action}Handler.cs       ← BaseCommandHandler<T>
    ├── Events/
    │   └── {Name}Event.cs           ← IEvent record
    ├── Models/                      ← Module-specific DTOs (optional)
    ├── Services/
    │   ├── I{Name}Service.cs        ← Interface
    │   └── {Name}Service.cs         ← Implementation (DB access here only)
    ├── ViewModels/
    │   └── {Name}ViewModel.cs       ← Inherits BaseViewModel
    ├── Views/
    │   ├── {Name}View.xaml          ← XAML
    │   └── {Name}View.xaml.cs       ← Code-behind (minimal)
    └── Workflows/                   ← Multi-step flows (optional)
        └── {Name}Workflow.cs        ← IWorkflow
```

---

## Data Flow Rules

### Writes (Commands)
```
View → ViewModel → CommandBus.SendAsync() → BaseCommandHandler → Service → DB
                                                    ↓
                                               EventBus.PublishAsync() → Subscribers
```

### Reads (Queries)
```
View → ViewModel → Service.GetAsync() → DB
```

### Cross-Module Communication
```
Module A (Handler) → publishes Event → EventBus → Module B (ViewModel subscribes)
```

---

## Base Class Rules

### BaseViewModel (all ViewModels)
- Inherit `BaseViewModel` — never `ObservableObject` directly
- Use `ErrorMessage` from base — never redeclare
- Use `IsLoading` from base — never redeclare
- Use `IsBusy` from base — never redeclare
- Use `RunAsync()` for automatic busy/error management

### BaseCommandHandler (all handlers)
- Inherit `BaseCommandHandler<TCommand>` — never `ICommandHandler<T>` directly
- Implement `ExecuteAsync()` — never `HandleAsync()`
- Return `CommandResult.Success()` or `CommandResult.Failure()` for expected outcomes
- Let the base catch unexpected exceptions

---

## Core Infrastructure — Do Not Redesign

These components are **frozen**. Extend through the existing interfaces only.

| Component | Location | Purpose |
|---|---|---|
| `AppStateService` | `Core/Services/` | Single source of truth for global state |
| `EventBus` | `Core/Events/` | Pub/sub for cross-module events |
| `CommandBus` | `Core/Commands/` | Dispatches commands to handlers |
| `WorkflowManager` | `Core/Workflows/` | Orchestrates multi-step user flows |
| `NavigationService` | `Core/Navigation/` | Page switching inside MainWindow |
| `FeatureToggleService` | `Core/Features/` | Feature flag management |
| `SessionService` | `Core/Session/` | Current user session state |
| `BaseViewModel` | `Core/Base/` | Base class for all ViewModels |
| `BaseCommandHandler` | `Core/Base/` | Base class for all command handlers |

---

## Registration Patterns

### Page Module (navigable content)
```csharp
public static IServiceCollection AddMyModule(
    this IServiceCollection services,
    NavigationPageRegistry pageRegistry)
{
    pageRegistry.Map<MyViewModel>("MyPage");
    services.AddSingleton<IMyService, MyService>();
    services.AddTransient<ICommandHandler<MyCommand>, MyHandler>();
    services.AddTransient<MyViewModel>();
    return services;
}
```

### Dialog Module (popup window)
```csharp
public static IServiceCollection AddMyModule(this IServiceCollection services)
{
    services.AddSingleton<IMyService, MyService>();
    services.AddTransient<MyViewModel>();
    services.AddTransient<MyWindow>();
    services.AddDialogRegistration<MyWindow>("MyDialog");
    return services;
}
```

---

## MVVM Boundaries

| Layer | Can Access | Cannot Access |
|---|---|---|
| **View (.xaml)** | ViewModel (via DataContext binding) | Services, DbContext, other Views |
| **ViewModel** | Services (via DI), CommandBus, EventBus | DbContext, Views, other ViewModels |
| **Service** | DbContext (via factory), other services | ViewModels, Views |
| **Command Handler** | Services (via DI), EventBus | ViewModels, Views, DbContext |

---

## Security Rules

- PINs stored as hashes only (via `PinHasher`)
- Master PIN required for Admin PIN changes and destructive operations
- Role checks in ViewModel before sending commands
- Never expose raw PIN values in events or logs
