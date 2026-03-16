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
- For fixed-size windows (e.g., Firm Management, User Management), avoid `ScrollViewer`; use scrolling only for data-growing views such as vendor lists and bills.
- Field widths: `FieldWidthCompact`, `FieldWidthStandard`, `FieldWidthWide` — never raw numbers.
- DataGrid columns: `ColumnWidthId`, `ColumnWidthPrice`, `ColumnWidthQty`, etc. One column must be `Width="*"`.

#### Layout Optimization

- App targets 1920×1080 resolution monitors. Layout decisions should be optimized for this resolution.

#### Dialog Field Sizing

- Input field sizes must match their content:
  - Address = single-line, full width (no `AcceptsReturn`).
  - PAN (10 chars) = `FieldWidthStandard`.
  - GSTIN (15 chars) = `FieldWidthWide`.
  - Pincode (6 digits) = `FieldWidthCompact`.
  - Phone (10–15 digits) = `FieldWidthStandard`.
  - Email/FirmName = full width.
  - ComboBoxes with month names = `FieldWidthStandard`.
- MaxLength in XAML **MUST** match MaxLength in Model and ViewModel — all three layers in sync.
- Dialog fields must be properly sized for their content — always check field widths and heights match expected data length.
- Dialog height must accommodate the tallest step without cropping.
- Step `Grid`s inside a `Height="*"` row must have `VerticalAlignment="Top"`.
- Animations: use `h:Motion.*` behaviors or `MotionSystem.xaml` storyboards — never inline.
- Tip banners: `InlineTipBanner` in page Row 1 with `TipBannerAutoState.TipKey` and `ContextKey`.

### Validation Error Display Pattern

- **GLOBAL RULE**: Never render validation error TEXT in the `Validation.ErrorTemplate` — it's in the adorner layer and ALWAYS overlaps elements below.
- The correct pattern is:
  1. `ErrorTemplate` = transparent pass-through (`<AdornedElementPlaceholder/>` only — no `Border`, no text) to avoid double-border with the control's own border.
  2. Style trigger on `Validation.HasError` sets `BorderBrush=FluentStrokeError` + `Background=FluentErrorBackground` (light red tint) + `ToolTip` with error text + `ToolTipService.InitialShowDelay=0`.
- This applies to ALL input controls: `TextBox`, `PasswordBox`, `ComboBox`, `DatePicker`.
- Already implemented in `GlobalStyles.xaml`.

### Validation Attribute Rules

- Every property with a validation attribute (`[MaxLength]`, `[Required]`, `[RegularExpression]`) **MUST** also have `[NotifyDataErrorInfo]` — otherwise the attribute is silently ignored by CommunityToolkit.Mvvm.
- Wizard/step dialogs **MUST NOT** call `ValidateAllProperties()` on Next — it causes **cross-step validation bleed** (errors on Step 2 block Step 1). Instead, use per-step validation: `ClearErrors()` for all properties, then `ValidateProperty()` for only the current step's fields.
- `ValidateAllProperties()` is only correct in `Save` — which validates everything before the final write.
- **Next validates only `[Required]` fields** — optional field format errors (regex, email, GSTIN, PAN) must never block step navigation. They are caught on Save.
- `SuccessMessage` and `ErrorMessage` must be cleared on step changes (Next/Back).
- When validation blocks Next, always set `ErrorMessage` so the user sees feedback.

### Wizard Dialog Rules

- For multi-step wizard dialogs, **never** bind `ConfirmCommand` directly to `SaveCommand`.
- Create a `ConfirmStepCommand` that delegates to `Next` on non-last steps and `Save` on the last step.
- Otherwise, Enter on the last field of any non-final step triggers Save prematurely (because `KeyboardNav` fires `DefaultCommand` when no next editable field exists in the visible step).
- When a hint `TextBlock` follows a validated field, there **MUST** be a `FormRowSpacing` row between them — otherwise the validation border overlaps the hint text.
- All hint text margins should be consistent across steps (`Margin="0,4,0,0"`).

### Data & regional

- EF Core via `IDbContextFactory<AppDbContext>` — short-lived contexts.
- Every async service method **MUST** accept `CancellationToken ct = default` as its last parameter and pass it to all EF Core calls.
- ViewModels forward `ct` from `RunAsync`/`RunLoadAsync` to service calls — never discard with `_`.
- DB-accessing services with no mutable state → `Transient`. Only state-holding services → `Singleton`.
- Financial writes require transactions (pipeline `[Transactional]`, `ITransactionSafetyService`, or `ITransactionHelper`).
- Timestamps: `IRegionalSettingsService.Now` (IST) — never `DateTime.Now`.
- `DateTime.UtcNow` only for DB-level comparisons (lockout expiry) and infrastructure logging.
- Formatting: `IRegionalSettingsService.FormatCurrency/Date/Time` — never hardcode format strings.
- Culture: `en-IN` set globally in `App.xaml.cs`.

### BaseViewModel IDisposable

- `BaseViewModel` implements `IDisposable` — disposes its internal `CancellationTokenSource`.
- Derived VMs that subscribe to events or hold resources **MUST** override `Dispose()` and call `base.Dispose()`.
- Never declare `IDisposable` redundantly on a VM — it already comes from `BaseViewModel`.

### Firm Management Dialog Preferences

- Number format is always Indian (1,00,000) — no field needed, not configurable.
- Currency is always ₹ INR — display only, no field needed.
- Date format needs more options.
- Watermark adorners must check `IsVisible` to prevent bleed-through when using Visibility toggling (already fixed in Watermark.cs). Watermark adorners (and any adorner-based helpers) MUST check `tb.IsVisible` before adding adorners, and MUST subscribe to `IsVisibleChanged` to remove adorners when elements become Collapsed. WPF adorners live in a separate rendering layer and do NOT automatically follow the Visibility of their adorned element.

### Regional Settings Integration

- `IRegionalSettingsService` is mutable — `CurrencySymbol` and `DateFormat` update at runtime via `UpdateSettings()`.
- On login: `SessionService.LoginAsync` reads `AppConfig` from DB and calls `regionalSettings.UpdateSettings()`.
- On firm save: `FirmUpdatedEvent` carries `CurrencySymbol` + `DateFormat`; `MainViewModel.OnFirmUpdatedAsync` calls `UpdateSettings()`.
- Any new regional setting added to Firm must follow this chain:
  1. Add to `AppConfig` model.
  2. Add to `FirmUpdateDto`.
  3. Save in `FirmService` with null/whitespace guard.
  4. Include in `FirmUpdatedEvent`.
  5. Refresh in `RegionalSettingsService.UpdateSettings()`.
  6. Refresh in `SessionService.LoginAsync` and `MainViewModel.OnFirmUpdatedAsync`.
- `SetupService.InitializeAppAsync` must explicitly set ALL `AppConfig` fields — never rely on EF defaults alone for business-critical fields.

### Window sizing

- Never set `Width`, `Height`, `ResizeMode`, or `WindowStartupLocation` in XAML.
- MainWindow: `WindowSizingService.ConfigureMainWindow` (maximized full screen, no resize).
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
- **Features Built**: 171 features out of 948 catalogued.
- **Pending Features**: 292 features pending (🔨).
- **Skipped Features**: 10 features skipped.
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

### Key Pending Areas

- **Phase 1**: Category hierarchy, paging, bulk operations, stock take, supplier enhancements.
- **Phase 2**: Cart tax display, split payment, credit notes, customer-sale linking, discount engine.
- **Phase 3**: Entire Expense, Cash Register, P&L modules.
- **Phase 4**: Audit log, permissions, backup/restore.
- **Phase 5**: Quotation, GRN, barcode operations, dashboard analytics.
- **Phase 6**: Print templates, accessibility, system settings.

### UI Preferences

- Remove visible Ctrl+Enter-style shortcut hints from UI everywhere.

### Feature Gaps

- Compare StoreAssistantPro UI against typical Shop Management apps to identify key gaps:
  - Cash Register/Day End
  - Quotation/Estimate
  - GRN
  - Barcode Label Printing
  - Customer-Sale linking
  - Split Payment
  - Credit Note/Exchange
  - A4 Tax Invoice
  - GSTR export
  - Audit Log
  - Backup/Restore
  - Dashboard Analytics
  - P&L Reports
  - Discount Engine
  - Print Templates

- The app has 27 modules, with approximately 171 features built out of 948 catalogued.
