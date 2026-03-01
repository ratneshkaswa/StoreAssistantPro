# Copilot Context — StoreAssistantPro

> **.NET 10 · WPF · MVVM · CommunityToolkit.Mvvm · Entity Framework Core**
>
> Enterprise Indian-retail desktop POS application.

## Mandatory documents — read before generating any code

| Priority | Document | Scope |
|---|---|---|
| 1 | [`MASTER_RULES.md`](../MASTER_RULES.md) | All rules — architecture, UI, layout, data, motion, help system, deep clean log |
| 2 | [`FEATURE_CATALOGUE.md`](../FEATURE_CATALOGUE.md) | All 948 features with status, phases, sprints, expansion gates |

## Rules

### Architecture

- Every new feature is a new module under `Modules/`.
- Modules depend only on `Core/`, `Models/`, and `Data/` — never on other modules.
- Cross-module communication uses `IEventBus` only.
- ViewModels inherit `BaseViewModel`. PIN dialogs inherit `PinPadViewModel`.
- Legacy command handlers inherit `BaseCommandHandler<T>`.
- Pipeline-aware handlers implement `ICommandRequestHandler<TCommand, TResult>`.
- Dialog windows inherit `BaseDialogWindow`. Content pages use `BasePage` as root.
- Business actions go through `ICommandBus.SendAsync` — never call services directly from ViewModels.
- The command pipeline (Validation → Logging → Offline → Transaction → Performance) is automatic for pipeline-aware handlers.
- Dialogs are shown via `IDialogService.ShowDialogAsync(dialogKey)` — never instantiate windows directly.

### Enterprise baseline — do not modify

These eight systems are frozen. Extend through existing interfaces only:

1. **Operational Modes** — `IAppStateService.CurrentMode` drives all mode-dependent visibility.
2. **Smart Billing Mode** — `SmartBillingModeService` with session lifecycle and safety interlocks.
3. **Focus Lock** — `IFocusLockService` gates navigation during active billing.
4. **Offline Safety** — `IConnectivityMonitorService` + `IOfflineModeService` + `OfflinePipelineBehavior`.
5. **Transaction Safety** — `ITransactionSafetyService` or `[Transactional]` attribute on commands.
6. **Command Pipeline** — `ICommandPipelineBehavior<,>` chain in registration order.
7. **Smart Help System** — `IContextHelpService`, `ITipRotationService`, `IOnboardingJourneyService`.
8. **Modern UI System** — `DesignSystem.xaml` → `FluentTheme.xaml` → `MotionSystem.xaml` → `GlobalStyles.xaml` → `PosStyles.xaml`.

### UI

- All visual values from `DesignSystem.xaml` — zero inline colors, margins, font sizes, corner radii.
- Use `StaticResource`, not `DynamicResource`.
- Buttons use named styles: `ToolbarButtonStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`.
- Page root: `Grid` with `Margin="{StaticResource PagePadding}"` and exactly one `Height="*"` row.
- Dialog root: `Grid` with `Margin="{StaticResource DialogPadding}"`, 3-row layout (Auto/*/Auto).
- `ScrollViewer` never wraps an entire window — only around data-driven content.
- Field widths: `FieldWidthCompact`, `FieldWidthStandard`, `FieldWidthWide` — never raw numbers.
- DataGrid columns: `ColumnWidthId`, `ColumnWidthPrice`, `ColumnWidthQty`, etc. One column must be `Width="*"`.
- Animations: use `h:Motion.*` behaviors or `MotionSystem.xaml` storyboards — never inline.
- Tip banners: `InlineTipBanner` in page Row 1 with `TipBannerAutoState.TipKey` and `ContextKey`.

### Data & regional

- EF Core via `IDbContextFactory<AppDbContext>` — short-lived contexts.
- Financial writes require transactions (pipeline `[Transactional]`, `ITransactionSafetyService`, or `ITransactionHelper`).
- Timestamps: `IRegionalSettingsService.Now` (IST) — never `DateTime.Now`.
- Formatting: `IRegionalSettingsService.FormatCurrency/Date/Time` — never hardcode format strings.
- Culture: `en-IN` set globally in `App.xaml.cs`.

### Window sizing

- Never set `Width`, `Height`, `ResizeMode`, or `WindowStartupLocation` in XAML.
- MainWindow: `WindowSizingService.ConfigureMainWindow` (90% of work area).
- Dialogs: override `DialogWidth`/`DialogHeight` in code-behind.
- Startup windows: `WindowSizingService.ConfigureStartupWindow`.

### DI registration

- Services: `Singleton`. Command handlers: `Transient`. ViewModels: `Transient`. Views: `Transient`.
- Each module exposes `Add<Name>Module()` called from `HostingExtensions`.
- Page modules register via `NavigationPageRegistry.Map<TViewModel>()`.
- Dialog modules register via `AddDialogRegistration<TWindow>()`.

## Feature Catalogue

> **Full catalogue**: [`FEATURE_CATALOGUE.md`](../FEATURE_CATALOGUE.md) — all 948 features with status.

### Core Features

- **Total Unique Features**: 948
- **Core Features**: 470 (focused on clothing retail, implement now)
- **Expansion Features**: 478 (gated by triggers)

### Current Progress

- **Deep clean completed**: All Phase 1–3 module code was stripped. Only core infrastructure + 5 shell modules remain.
- **Active Modules**: Authentication, Firm, MainShell, Startup, Users.
- **Features Built**: Infrastructure + shell features only. Module features will be rebuilt from Phase 1.
- **Skipped Features**: 2 (MRP, SKU).
- **Current Phase**: Restarting from Phase 1 — Product Foundation.
- **Next Features**: Phase 1 Sprint 1 (Products module rebuild).
- **Cleanup Log**: `MASTER_RULES.md §9` — full audit of what was stripped and why.

### Phases and Sprints

- **Core Features Organization**: 6 phases / 28 sprints across 22 windows (12 new, 6 existing enrichment).
- **Expansion Features Organization**: 22 gates:
  - G1: Hardware (48)
  - G2: AI (22)
  - G3: Commercial (27)
  - G4: Localization (23)
  - G5: MultiStore (49)
  - G6: Ecommerce (46)
  - G7: API (21)
  - G8: Mobile (11)
  - G9: MultiCountry (9)
  - G10: Niche Vertical (61)
  - G11: Touch (11)
  - G12: CRM (25)
  - G13: HR (8)
  - G14: Compliance (17)
  - G15: UI Polish (18)
  - G16: DB Admin (14)
  - G17: Documents (9)
  - G18: Workflows (8)
  - G19: Preferences (19)
  - G20: Payments (17)
  - G21: Budgeting (6)
  - G22: Reporting (9)
