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
| `BaseDialogWindow` | `Core/Base/` | Base class for all dialog windows |
| `WindowSizingService` | `Core/Services/` | Enterprise window sizing rules |
| `RegionalSettingsService` | `Core/Services/` | Indian regional formatting (en-IN, IST) |

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

## Window Sizing Rules — Do Not Override

All window sizing is controlled by `IWindowSizingService`. **Never set `Height`, `Width`, `ResizeMode`, or `WindowStartupLocation` in XAML** for windows managed by the service.

| Window Type | Size | Position | Resize | Base Class |
|---|---|---|---|---|
| **MainWindow** | 90% of screen working area | Centered on screen | Disabled | `Window` |
| **Dialog windows** | Fixed (declared via `DialogWidth`/`DialogHeight`) | Centered over MainWindow | Disabled | `BaseDialogWindow` |
| **Startup windows** | Fixed (passed to `ConfigureStartupWindow`) | Centered on screen | Disabled | `Window` |

### Rules

- All windows are **fixed size** — users cannot resize any window
- MainWindow is always **90% of `SystemParameters.WorkArea`** (excludes taskbar)
- MainWindow **auto-resizes** on display resolution/DPI changes
- Dialog windows are always **smaller than MainWindow**
- Dialog windows always have **MainWindow as Owner** (taskbar grouping + CenterOwner)
- New dialog windows **must inherit `BaseDialogWindow`** and declare their size
- Startup/auth windows use `ConfigureStartupWindow` (no owner exists during login)
- `WindowSizingService` is a **frozen singleton** — extend, never rewrite

### Creating a New Dialog Window

```csharp
// Code-behind:
public partial class MyWindow : BaseDialogWindow
{
    protected override double DialogWidth => 500;
    protected override double DialogHeight => 400;

    public MyWindow(IWindowSizingService sizing, MyViewModel vm) : base(sizing)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
```

```xml
<!-- XAML (no sizing attributes): -->
<core:BaseDialogWindow x:Class="...MyWindow"
        xmlns:core="clr-namespace:StoreAssistantPro.Core"
        Title="My Dialog">
    <!-- content -->
</core:BaseDialogWindow>
```

---

## Regional & Culture Rules — Indian Retail Defaults

Global culture is set to `en-IN` in `App.xaml.cs` before any other code runs. **All formatting must go through `IRegionalSettingsService`.**

### Global Culture (`App.xaml.cs → SetIndianCulture`)

| Setting | Value | Scope |
|---|---|---|
| `CultureInfo.CurrentCulture` | `en-IN` | UI thread |
| `CultureInfo.DefaultThreadCurrentCulture` | `en-IN` | All threads (Task.Run, async) |
| `FrameworkElement.LanguageProperty` | `en-IN` | XAML `StringFormat` bindings |

### RegionalSettingsService (singleton)

| Helper | Example Output | Use Case |
|---|---|---|
| `FormatCurrency(1234567.89m)` | `₹12,34,567.89` | Prices, totals, invoices |
| `FormatNumber(1234567)` | `12,34,567` | Quantities, counts |
| `FormatQuantity(5.00m)` | `5` (drops `.00`) | Stock, cart items |
| `FormatPercent(18.5m)` | `18.50 %` | GST, discounts |
| `FormatDate(dt)` | `19-02-2026` | Sale dates, reports |
| `FormatTime(dt)` | `02:30 PM` | Status bar, lockout messages |
| `Now` | IST (`Asia/Kolkata`) | All timestamps |

### Rules

- **Never use `DateTime.Now`** in services or handlers — use `regional.Now` (IST)
- `DateTime.UtcNow` is allowed **only** for DB-level comparisons (e.g., lockout expiry)
- **Never hardcode format strings** like `"hh:mm tt"` or `"dd-MM-yyyy"` — use the service
- XAML `StringFormat=C` / `StringFormat=g` is fine — it reads from the global `CultureInfo`
- Indian number grouping (lakhs/crores) is automatic via `en-IN` culture
- `RegionalSettingsService` is a **frozen singleton** — extend, never rewrite

### AppConfig Business Fields

| Field | Default | Purpose |
|---|---|---|
| `CurrencyCode` | `"INR"` | ISO 4217 currency code |
| `FinancialYearStartMonth` | `4` (April) | Indian FY start |
| `FinancialYearEndMonth` | `3` (March) | Indian FY end |
| `GSTNumber` | `null` | Future: GSTIN for invoices |

---

## Financial Transaction Rules — Mandatory Before Billing

All financial operations **must** execute inside a database transaction. No exceptions.

### Required Pattern

```csharp
await using var strategySource = await contextFactory.CreateDbContextAsync();
var strategy = strategySource.Database.CreateExecutionStrategy();

await strategy.ExecuteAsync(async () =>
{
    await using var context = await contextFactory.CreateDbContextAsync();
    await using var transaction = await context.Database.BeginTransactionAsync();

    // ... all DB writes here ...

    await context.SaveChangesAsync();
    await transaction.CommitAsync();
});
```

### Rules

- **Every** operation that modifies financial data (sales, payments, stock adjustments, refunds) must use `BeginTransactionAsync` + `CommitAsync`
- Transactions must be wrapped in `CreateExecutionStrategy().ExecuteAsync()` for SQL Server retry compatibility
- The work context must be created **inside** the retry lambda so retries start with a clean context
- Concurrency conflicts (`DbUpdateConcurrencyException`) must be caught and reported to the user
- **Never** call `SaveChangesAsync()` outside a transaction for financial writes
- Read-only queries (`GetAllAsync`, reports) do **not** need transactions

### Operations Requiring Transactions

| Operation | Service | Status |
|---|---|---|
| Create sale + deduct stock | `SalesService.CreateSaleAsync` | ✅ Implemented |
| Process refund | Future `RefundService` | Requires transaction |
| Adjust stock manually | Future `StockAdjustmentService` | Requires transaction |
| Process payment | Future `PaymentService` | Requires transaction |
| Billing invoice creation | Future `BillingService` | Requires transaction |

---

## Security Rules

- PINs stored as hashes only (via `PinHasher`)
- Master PIN required for Admin PIN changes and destructive operations
- Role checks in ViewModel before sending commands
- Never expose raw PIN values in events or logs
