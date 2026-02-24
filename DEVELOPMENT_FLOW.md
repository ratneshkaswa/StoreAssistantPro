# StoreAssistantPro — Development Flow

> **Rule: New Feature = New Module.**
> Follow this checklist every time. Never skip a step.

---

## Enterprise Architecture Baseline

> **MANDATORY BASELINE — DO NOT MODIFY**
>
> The following eight systems form the architectural foundation of
> StoreAssistantPro. They are fully implemented, production-tested,
> and frozen. New features **must build on top of** these systems
> using the existing interfaces. Never redesign, replace, or bypass
> any baseline component.

### 1. Operational Modes

Dual-mode architecture drives feature visibility, navigation, toolbar
composition, and keyboard shortcut sets across the entire shell.

| Component | Location | Role |
|---|---|---|
| `OperationalMode` | `Models/` | `Management` · `Billing` enum |
| `IAppStateService` | `Core/Services/` | Stores current mode; publishes `OperationalModeChangedEvent` |
| `IBillingModeService` | `Modules/Billing/Services/` | Executes mode transitions (enter/exit billing) |

**Rules:**
- All mode-dependent visibility is driven by `IAppStateService.CurrentMode`.
- Mode changes flow through `IEventBus` — never poll for state.
- Navigation, sidebar, and toolbar react automatically via event subscriptions.

### 2. Smart Billing Mode

State-machine-driven billing session lifecycle with safety interlocks
that prevent data loss during active sales.

| Component | Location | Role |
|---|---|---|
| `SmartBillingModeService` | `Modules/Billing/Services/` | Reacts to session events; drives `IBillingModeService` |
| `IBillingSessionService` | `Modules/Billing/Services/` | Manages `BillingSessionState` lifecycle |
| `BillingAutoSaveService` | `Modules/Billing/Services/` | Debounced cart persistence; immediate flush on payment |
| `IBillingResumeService` | `Modules/Billing/Services/` | Recovers interrupted sessions on login |
| `IStaleBillingSessionCleanupService` | `Modules/Billing/Services/` | Archives abandoned sessions at startup |

**Safety interlocks:**
- Active-session guard — cannot exit Billing while session is active.
- Payment lock — mode transitions blocked during payment processing.
- Focus lock hold — UI navigation frozen mid-payment.
- Serialized transitions — `SemaphoreSlim` prevents race conditions.

### 3. Focus Lock

Module-level UI focus locking that prevents accidental context switches
during active billing sessions.

| Component | Location | Role |
|---|---|---|
| `IFocusLockService` | `Core/Services/` | `IsFocusLocked` · `ActiveModule` · `IsReleaseHeld` |
| `FocusLockService` | `Core/Services/` | Implements lock/release/hold lifecycle |

**Rules:**
- ViewModels read `IsFocusLocked` and `ActiveModule` to gate navigation.
- Only `SmartBillingModeService` and `FocusLockService` control transitions.
- `HoldRelease()` defers unlock during payment; `LiftReleaseHold()` flushes.
- Implements `INotifyPropertyChanged` so XAML can bind directly.

### 4. Offline Safety

Database connectivity monitoring with automatic mode switching and
graceful degradation during outages.

| Component | Location | Role |
|---|---|---|
| `IConnectivityMonitorService` | `Core/Services/` | Background heartbeat timer; publishes `ConnectionLost`/`Restored` events |
| `IOfflineModeService` | `Core/Services/` | Reacts to connectivity events; updates `IAppStateService.IsOfflineMode` |
| `OfflinePipelineBehavior` | `Core/Commands/Offline/` | Rejects DB-dependent commands when offline |

**Rules:**
- Connectivity events flow through `IEventBus` — never poll.
- `OfflineModeChangedEvent` drives help text suffixes, tip banners, and status bar messages.
- Billing continues locally with auto-save; syncs on reconnect.

### 5. Transaction Safety

Structured database transaction boundaries with result-based error
reporting for all financial operations.

| Component | Location | Role |
|---|---|---|
| `ITransactionHelper` | `Core/Services/` | Exception-based: begin → commit or throw |
| `ITransactionSafetyService` | `Core/Services/` | Result-based: returns `TransactionResult` (no exceptions) |
| `TransactionPipelineBehavior` | `Core/Commands/Transaction/` | Automatic transaction wrapping for commands marked `[Transactional]` |

**Rules:**
- All financial writes use execution strategy + transaction (begin → commit).
- Work context created inside retry lambda for clean retry.
- `DbUpdateConcurrencyException` caught and reported to user.
- Read-only queries do not need transactions.

### 6. Command Pipeline

Middleware-based command execution with cross-cutting concerns applied
in registration order as a Russian-doll chain.

| Behavior | Location | Order | Purpose |
|---|---|---|---|
| `ValidationPipelineBehavior` | `Core/Commands/Validation/` | 1 | Validates command properties; short-circuits on failure |
| `LoggingPipelineBehavior` | `Core/Commands/Logging/` | 2 | Logs command name, duration, success/failure |
| `OfflinePipelineBehavior` | `Core/Commands/Offline/` | 3 | Rejects DB-dependent commands when offline |
| `TransactionPipelineBehavior` | `Core/Commands/Transaction/` | 4 | Wraps `[Transactional]` commands in safe boundary |
| `PerformancePipelineBehavior` | `Core/Commands/Performance/` | 5 | Measures execution time; warns on slow commands |

**Rules:**
- Behaviors are cross-cutting — no business logic specific to one command.
- Behaviors must call `next()` exactly once (or return early to short-circuit).
- Registered as open generics: `services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(...))`.
- Execution order matches DI registration order.

### 7. Smart Help System

Context-aware help, onboarding guidance, and experience-level adaptive
content across tooltips, tip banners, and help overlays.

| Component | Location | Role |
|---|---|---|
| `IContextHelpService` | `Core/Services/` | Rule pipeline → context-specific help text; experience-level adaptation |
| `ITipRotationService` | `Core/Services/` | Selects next tip per window; experience-gated frequency |
| `ITipRegistryService` | `Core/Services/` | Stores all `TipDefinition` registrations |
| `IOnboardingJourneyService` | `Core/Services/` | Tracks milestones; auto-promotes experience level |
| `IUserInteractionTracker` | `Core/Services/` | Counts distinct windows/sessions for promotion |
| `ITipStateService` | `Core/Services/` | Persists per-tip dismiss state to JSON file |
| `OnboardingTipRegistrar` | `Core/Services/` | Registers beginner onboarding tips at startup |
| `SmartTooltip` | `Core/Helpers/` | Context-aware tooltips via attached properties |
| `InlineTipBanner` | `Core/Controls/` | Per-page tip banners with dismiss persistence |
| `TipBannerAutoState` | `Core/Helpers/` | Attached behavior connecting banners to state/help services |

**Rules:**
- Help text auto-refreshes on `OperationalModeChangedEvent`, `OfflineModeChangedEvent`,
  `ExperienceLevelPromotedEvent`, and `FocusLockService.PropertyChanged`.
- Tip banners always in page Row 1 (below title, above toolbar).
- Onboarding tips: `TipLevel.Beginner`, `Priority = 90`, `IsOneTime = true`.
- New help keys require rule pipeline entry + `ExperienceLevelAdapter` entries.

### 8. Modern UI System

Fluent-inspired design system with centralized tokens, motion system,
and development-time compliance enforcement.

| Component | Location | Role |
|---|---|---|
| `DesignSystem.xaml` | `Core/Styles/` | Pure tokens: colors, spacing, sizing, typography, motion |
| `FluentTheme.xaml` | `Core/Styles/` | Keyed control templates consuming tokens |
| `MotionSystem.xaml` | `Core/Styles/` | Reusable storyboards + motion styles |
| `GlobalStyles.xaml` | `Core/Styles/` | Implicit styles + named app styles (`BasedOn` Fluent) |
| `PosStyles.xaml` | `Core/Styles/` | POS-specific templates (keypad, role buttons) |
| `Motion.cs` | `Core/Helpers/` | `h:Motion.*` attached behaviors (fade, slide, scale) |
| `StyleComplianceDiagnostics.cs` | `Core/Helpers/` | DEBUG-only: detects inline colors, margins, font sizes |
| `LayoutDiagnostics.cs` | `Core/Helpers/` | DEBUG-only: detects illegal `ScrollViewer` wrapping |

**Rules:**
- All visual values from `DesignSystem.xaml` — zero inline colors, margins, font sizes.
- Styles loaded in strict order: DesignSystem → FluentTheme → MotionSystem → GlobalStyles → PosStyles.
- `StaticResource` only (not `DynamicResource`) for tokens.
- Animations use `h:Motion.*` behaviors or `MotionSystem.xaml` storyboards — never inline.
- Every view uses `EnterpriseDataGridStyle`, named button styles, and card styles.

### Baseline integration map

```
┌─ Operational Modes ────────────────────────────────────────────┐
│  AppStateService.CurrentMode                                   │
│       │                                                        │
│       ├──▶ Smart Billing Mode (session lifecycle + interlocks) │
│       │        │                                               │
│       │        ├──▶ Focus Lock (navigation gating)             │
│       │        └──▶ Auto-Save (debounced cart persistence)     │
│       │                                                        │
│       ├──▶ Offline Safety (connectivity → graceful degrade)    │
│       │                                                        │
│       ├──▶ Smart Help System (context-aware guidance)          │
│       │        ├── ContextHelpService (rule pipeline)          │
│       │        ├── TipRotationService (experience-gated)       │
│       │        └── OnboardingJourneyService (auto-promote)     │
│       │                                                        │
│       └──▶ Modern UI System (mode-driven visibility)           │
│                                                                │
├─ Command Pipeline ─────────────────────────────────────────────┤
│  Validation → Logging → Offline Guard → Transaction → Perf     │
│                                                                │
├─ Transaction Safety ───────────────────────────────────────────┤
│  ExecutionStrategy → BeginTransaction → Commit/Rollback        │
└────────────────────────────────────────────────────────────────┘
```

---

## Development Checklist

| # | Step | Output |
|---|---|---|
| 1 | Create module folder | `Modules/{ModuleName}/` |
| 2 | Create models | `Models/` or `Modules/{Module}/Models/` |
| 3 | Create service interface + implementation | `Modules/{Module}/Services/I{Name}Service.cs` + `{Name}Service.cs` |
| 4 | Create commands + handlers | `{Name}Command.cs` : `ICommandRequest<TResult>` + `{Name}Handler.cs` : `ICommandRequestHandler<,>` |
| 5 | Create command validator (if needed) | `{Name}Validator.cs` : `ICommandValidator<TCommand>` |
| 6 | Create ViewModel | `Modules/{Module}/ViewModels/{Name}ViewModel.cs` — inherits `BaseViewModel` |
| 7 | Create View | `Modules/{Module}/Views/{Name}View.xaml` + `.xaml.cs` |
| 8 | Register in DI | `Modules/{Module}/{Module}Module.cs` — services, handlers, validators, ViewModels, Views |
| 9 | Add navigation / dialog entry | Register page in `NavigationPageRegistry` or dialog via `AddDialogRegistration<TWindow>()` |
| 10 | Add events if needed | `Modules/{Module}/Events/{Name}Event.cs` — publish from handler, subscribe from ViewModel |
| 11 | Add tip banner + help keys | `InlineTipBanner` in page Row 1; register tip definition; add `SmartTooltip` properties |
| 12 | Add DataTemplate | Implicit `DataTemplate` in `App.xaml` for ViewModel → View mapping (pages only) |
| 13 | Add feature flag | `Core/Features/FeatureFlags.cs` + `appsettings.json` → bind visibility in XAML |
| 14 | Write tests | `Tests/Commands/`, `Tests/ViewModels/` — one test class per handler + ViewModel |

---

## Module Structure Template

```
Modules/
└── {ModuleName}/
    ├── {ModuleName}Module.cs        ← DI registration
    ├── Commands/
    │   ├── {Action}Command.cs       ← ICommandRequest<TResult> record (pipeline) or ICommand (legacy)
    │   ├── {Action}Handler.cs       ← ICommandRequestHandler<,> (pipeline) or BaseCommandHandler<T> (legacy)
    │   └── {Action}Validator.cs     ← ICommandValidator<T> (optional, pipeline only)
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
View → ViewModel → CommandBus.SendAsync()
                        ↓
                   Pipeline (Validation → Logging → Offline → Transaction → Performance)
                        ↓
                   Handler.ExecuteAsync() → Service → DB
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

### BaseCommandHandler (legacy handlers)
- Inherit `BaseCommandHandler<TCommand>` — never `ICommandHandler<T>` directly
- Implement `ExecuteAsync()` — never `HandleAsync()`
- Return `CommandResult.Success()` or `CommandResult.Failure()` for expected outcomes
- Let the base catch unexpected exceptions

### ICommandRequestHandler (pipeline-aware handlers)
- Implement `ICommandRequestHandler<TCommand, TResult>` for new commands
- Commands implement `ICommandRequest<TResult>`
- Pipeline behaviors (validation, logging, offline, transaction, perf) wrap automatically
- Use `ICommandValidator<TCommand>` for pre-execution validation
- Use `[Transactional]` marker interface for automatic transaction wrapping

### PinPadViewModel (PIN entry reuse)
- Inherit `PinPadViewModel` for any dialog requiring numeric PIN entry
- Provides digit entry, backspace, clear, max-length enforcement, `PinCompleted` callback

---

## Core Infrastructure — Do Not Redesign

These components are **frozen**. Extend through the existing interfaces only.

| Component | Location | Purpose |
|---|---|---|
| `AppStateService` | `Core/Services/` | Single source of truth for global state (mode, offline, session) |
| `EventBus` | `Core/Events/` | Pub/sub for cross-module events |
| `CommandBus` | `Core/Commands/` | Dispatches commands to handlers via pipeline |
| `CommandExecutionPipeline` | `Core/Commands/` | Ordered middleware chain (validation → logging → offline → transaction → perf) |
| `WorkflowManager` | `Core/Workflows/` | Orchestrates multi-step user flows |
| `NavigationService` | `Core/Navigation/` | Page switching inside MainWindow |
| `FeatureToggleService` | `Core/Features/` | Feature flag management |
| `SessionService` | `Core/Session/` | Current user session state |
| `FocusLockService` | `Core/Services/` | Module-level UI focus locking |
| `ConnectivityMonitorService` | `Core/Services/` | Background DB heartbeat; connectivity events |
| `OfflineModeService` | `Core/Services/` | Offline mode switching; status bar messages |
| `TransactionSafetyService` | `Core/Services/` | Result-based safe transaction boundaries |
| `TransactionHelper` | `Core/Services/` | Exception-based transaction helper |
| `ContextHelpService` | `Core/Services/` | Context-aware help rule pipeline; experience-level adaptation |
| `TipRotationService` | `Core/Services/` | Experience-gated tip selection per window |
| `TipRegistryService` | `Core/Services/` | Stores all `TipDefinition` registrations |
| `OnboardingJourneyService` | `Core/Services/` | Milestone tracking; auto-promotes experience level |
| `UserInteractionTracker` | `Core/Services/` | Counts distinct windows/sessions for promotion |
| `TipStateService` | `Core/Services/` | Persists per-tip dismiss state to JSON |
| `OnboardingTipRegistrar` | `Core/Services/` | Registers beginner onboarding tips at startup |
| `NotificationService` | `Core/Services/` | In-app notification management |
| `StatusBarService` | `Core/Services/` | Status bar message posting with auto-clear |
| `PerformanceMonitor` | `Core/Services/` | Runtime performance measurement |
| `WindowRegistry` | `Core/Services/` | Dialog-key → Window-type mapping for `IDialogService` |
| `DialogService` | `Modules/MainShell/Services/` | Shows modal dialogs by key via `IWindowRegistry` |
| `FileLoggerProvider` | `Core/Services/` | File-based log provider |
| `MasterPinValidator` | `Core/Services/` | Validates master PIN for admin operations |
| `ApplicationInfoService` | `Core/Services/` | App version, build info |
| `BaseViewModel` | `Core/Base/` | Base class for all ViewModels |
| `PinPadViewModel` | `Core/Base/` | Reusable PIN pad logic |
| `BaseCommandHandler` | `Core/Base/` | Base class for legacy command handlers |
| `BaseDialogWindow` | `Core/Base/` | Base class for all dialog windows |
| `BasePage` | `Core/Base/` | Base class for all content pages |
| `WindowSizingService` | `Core/Services/` | Enterprise window sizing rules |
| `RegionalSettingsService` | `Core/Services/` | Indian regional formatting (en-IN, IST) |
| `PricingCalculationService` | `Core/Services/` | Per-line price/tax calculation |
| `BillCalculationService` | `Core/Services/` | Whole-bill subtotal/discount/tax |
| `TaxCalculationService` | `Core/Services/` | GST component split (CGST/SGST/IGST) |

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

### Pipeline-Aware Module (with command validation)
```csharp
public static IServiceCollection AddMyModule(this IServiceCollection services)
{
    services.AddTransient<IMyService, MyService>();
    services.AddTransient<ICommandRequestHandler<MyCommand, Unit>, MyCommandHandler>();
    services.AddTransient<ICommandValidator<MyCommand>, MyCommandValidator>();
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

## Financial Transaction Rules

All financial operations **must** execute inside a database transaction. No exceptions.

### Available approaches

| Approach | Service | Error model | Use when |
|---|---|---|---|
| **Pipeline** | `TransactionPipelineBehavior` | `CommandResult.Failure` | Command marked `[Transactional]` — automatic |
| **Result-based** | `ITransactionSafetyService` | `TransactionResult` (inspect `.Succeeded`) | Service-layer orchestration with structured results |
| **Exception-based** | `ITransactionHelper` | Throws on failure | Legacy pattern; still valid |

### Manual pattern (when not using pipeline or safety service)

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
- Prefer `[Transactional]` attribute on commands for automatic pipeline wrapping

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
