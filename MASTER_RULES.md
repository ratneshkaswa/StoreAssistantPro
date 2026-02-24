# StoreAssistantPro — Master Rules

> **.NET 10 · WPF · MVVM · CommunityToolkit.Mvvm · Entity Framework Core**
>
> Central reference for all development standards in this enterprise
> Indian-retail WPF application. Every contributor and every AI agent
> must follow these rules when adding or modifying code.
>
> **Canonical sources** (this document summarises — the originals are
> authoritative):
>
> | Document | Scope |
> |---|---|
> | [`ARCHITECTURE.md`](ARCHITECTURE.md) | Solution structure, MVVM wiring, base classes, keyboard nav, pricing rules |
> | [`UI_RULES.md`](UI_RULES.md) | Design system, styles, layout, scroll policy, form density, motion, tip system, prohibited patterns |
> | [`DEVELOPMENT_FLOW.md`](DEVELOPMENT_FLOW.md) | Enterprise architecture baseline, module creation checklist, data flow, DI patterns, regional/financial rules |

---

## 1  Architecture Rules

> Full specification: [`ARCHITECTURE.md` §1–4, §9–11](ARCHITECTURE.md)
> and [`DEVELOPMENT_FLOW.md`](DEVELOPMENT_FLOW.md)

### 1.1  Solution structure

```
Core/        → shared infrastructure (no module dependencies)
Data/        → EF Core DbContext + Migrations
Models/      → domain entities
Modules/     → vertical feature slices (one per business capability)
Templates/   → XAML page/dialog scaffolding templates
```

### 1.2  Module isolation

| Rule | Ref |
|---|---|
| A module may only depend on `Core/`, `Models/`, and `Data/`. | `ARCHITECTURE.md §2` |
| Modules **never** reference another module directly. | `ARCHITECTURE.md §2` |
| Cross-module communication uses `IEventBus` only. | `ARCHITECTURE.md §10` |
| Each module exposes a single `Add<Name>Module()` extension method. | `DEVELOPMENT_FLOW.md §Registration` |

### 1.3  MVVM boundaries

| Layer | Can Access | Cannot Access |
|---|---|---|
| **View (.xaml)** | ViewModel (via DataContext binding) | Services, DbContext, other Views |
| **ViewModel** | Services (via DI), CommandBus, EventBus | DbContext, Views, other ViewModels |
| **Service** | DbContext (via factory), other services | ViewModels, Views |
| **Command Handler** | Services (via DI), EventBus | ViewModels, Views, DbContext |

> `DEVELOPMENT_FLOW.md §MVVM Boundaries`

### 1.4  Base classes (mandatory)

| Class | Inheritors | Provides |
|---|---|---|
| `BaseViewModel` | All ViewModels | `IsBusy`, `IsLoading`, `ErrorMessage`, `Validate()`, `RunAsync()` |
| `PinPadViewModel` | PIN entry dialogs | Digit entry, backspace, clear, `PinCompleted` callback |
| `BaseCommandHandler<T>` | Legacy command handlers | `ExecuteAsync()` template, error capture |
| `ICommandRequestHandler<TCmd,TResult>` | Pipeline-aware handlers | Full pipeline (validation → logging → offline → transaction → perf) |
| `BaseDialogWindow` | All modal dialogs | Fixed sizing, Enter/Esc wiring, owner centering |
| `BasePage` | All content pages | Title bar, error bar, loading overlay, 20 px padding |

> `ARCHITECTURE.md §4`, `DEVELOPMENT_FLOW.md §Base Class Rules`

### 1.5  Frozen infrastructure

These components are **immutable** — extend through existing interfaces only,
never redesign:

**Core services:** `AppStateService` · `EventBus` · `CommandBus` ·
`CommandExecutionPipeline` · `WorkflowManager` · `NavigationService` ·
`FeatureToggleService` · `SessionService` · `FocusLockService` ·
`ConnectivityMonitorService` · `OfflineModeService` · `NotificationService` ·
`PerformanceMonitor` · `WindowSizingService` · `RegionalSettingsService`

**Transaction safety:** `TransactionSafetyService` · `TransactionHelper` ·
`TransactionPipelineBehavior`

**Help system:** `ContextHelpService` · `TipRotationService` ·
`TipRegistryService` · `TipStateService` · `OnboardingJourneyService` ·
`UserInteractionTracker` · `OnboardingTipRegistrar`

**Pricing:** `PricingCalculationService` · `BillCalculationService` ·
`TaxCalculationService`

**Base classes:** `BaseViewModel` · `PinPadViewModel` ·
`BaseCommandHandler` · `BaseDialogWindow` · `BasePage`

> `DEVELOPMENT_FLOW.md §Core Infrastructure`

### 1.6  DI lifetime rules

| Category | Lifetime |
|---|---|
| Services (state, session, settings) | `Singleton` |
| Legacy command handlers (`ICommandHandler<T>`) | `Transient` |
| Pipeline-aware handlers (`ICommandRequestHandler<,>`) | `Transient` |
| Command validators (`ICommandValidator<T>`) | `Transient` |
| Pipeline behaviors (`ICommandPipelineBehavior<,>`) | `Transient` (open generic) |
| Workflows (`IWorkflow`) | `Singleton` |
| ViewModels | `Transient` |
| Views / Windows | `Transient` |
| `DbContextFactory` | `Singleton` (individual `DbContext` short-lived) |

> `ARCHITECTURE.md §9`

### 1.7  Data access

- EF Core with `IDbContextFactory<AppDbContext>`.
- Short-lived `DbContext` via `ITransactionHelper` for unit-of-work scoping.
- Financial writes **must** use transactions via one of:
  - `[Transactional]` attribute on commands (automatic pipeline wrapping).
  - `ITransactionSafetyService` (result-based, no exceptions).
  - `ITransactionHelper` (exception-based).
  - Manual `BeginTransactionAsync` + `CommitAsync` inside
    `CreateExecutionStrategy().ExecuteAsync()`.
- Read-only queries do not need transactions.

> `DEVELOPMENT_FLOW.md §Financial Transaction Rules`

---

## 2  UI Rules

> Full specification: [`UI_RULES.md`](UI_RULES.md)

### 2.1  Design system is the single source of truth

All visual values come from `Core/Styles/DesignSystem.xaml`. Zero inline
values are permitted:

| Forbidden | Required |
|---|---|
| `Foreground="#616161"` | `Foreground="{StaticResource FluentTextSecondary}"` |
| `Margin="0,0,0,12"` | `Margin="{StaticResource FieldGroupSpacing}"` |
| `FontSize="13"` | `FontSize="{StaticResource FontSizeBody}"` |
| `CornerRadius="8"` | `CornerRadius="{StaticResource FluentCornerMedium}"` |
| `Width="80"` on a field | `Width="{StaticResource FieldWidthCompact}"` |

If a needed token does not exist, add it to `DesignSystem.xaml` — never
hard-code at the call site.

> `UI_RULES.md §1, §11`

### 2.2  Style architecture (load order)

```
DesignSystem.xaml   → pure tokens (colors, spacing, sizing, typography, motion)
  ↓
FluentTheme.xaml    → keyed control templates consuming tokens
  ↓
MotionSystem.xaml   → reusable Storyboards + motion styles
  ↓
GlobalStyles.xaml   → implicit styles + named app styles (BasedOn Fluent)
  ↓
PosStyles.xaml      → POS-specific templates (keypad, role buttons)
```

- Tokens live **only** in `DesignSystem.xaml`.
- Control templates live **only** in `FluentTheme.xaml` or `PosStyles.xaml`.
- Named app styles live in `GlobalStyles.xaml` with `BasedOn` inheritance.
- Views must **never** define inline styles — use `StaticResource` references.
- Use `StaticResource`, not `DynamicResource` (tokens don't change at runtime).

> `UI_RULES.md §2`

### 2.3  Named styles (never raw controls)

**Buttons:** `ToolbarButtonStyle` · `PrimaryButtonStyle` · `SecondaryButtonStyle` ·
`PosKeypadButtonStyle` · `SelectableUserButtonStyle`

**Typography:** `PageTitleStyle` · `DialogTitleStyle` · `SectionHeaderStyle` ·
`FieldLabelStyle` · `FormRowLabelStyle` · `CaptionLabelStyle` ·
`ErrorMessageStyle` · `SuccessMessageStyle`

**Containers:** `FormCardStyle` · `SectionCardStyle` · `SectionCardHeaderStyle` ·
`DetailPanelStyle` · `SidebarNavStyle` · `StatCardStyle` · `OverlayCardStyle`

**Data grids:** implicit style for basic, `EnterpriseDataGridStyle` for primary
data tables with full virtualisation.

> `UI_RULES.md §3–4`

### 2.4  Color semantics

Never pick arbitrary colors. Use the semantic palette:

| Role | Token |
|---|---|
| Primary text | `FluentTextPrimary` |
| Secondary text | `FluentTextSecondary` |
| Page background | `FluentBackgroundPrimary` |
| Card surface | `FluentSurface` |
| Brand accent | `FluentAccentDefault` / `Hover` / `Pressed` |
| Success / Warning / Error | `FluentSuccess` · `FluentWarning` · `FluentError` |

> `UI_RULES.md §7`

### 2.5  Layout rules

- **Grid is the primary layout container** — always.
- Every page must have **exactly one `*`-sized row** for the primary
  data area.
- `ScrollViewer` must **never** wrap an entire Window, Dialog, or
  UserControl root — only around data-driven content.
- Window sizing is **always programmatic** — never in XAML.
- Use spacing tokens from `DesignSystem.xaml` — no magic numbers.

> `ARCHITECTURE.md §5.1–5.4`, `UI_RULES.md §5 Layout Standards`

### 2.6  Page structure template

```
Row 0  Auto   Page title          (PageTitleStyle)
Row 1  Auto   Tip banner          (InlineTipBanner, context-adaptive)
Row 2  Auto   Toolbar / filters   (ToolbarSpacing)
Row 3  Auto   Inline form         (FormCardStyle, collapsible)
Row 4  Auto   Error/success       (ErrorMessageStyle / SuccessMessageStyle)
Row 5  *      Primary data area   (DataGrid or ScrollViewer)
```

> `UI_RULES.md §5.4 Standard Page Layout`

### 2.7  Dialog structure template

```
Row 0  Auto   Title               (DialogTitleStyle)
Row 1  *      Form body           (fields + messages)
Row 2  Auto   Action buttons      (right-aligned, Primary + Secondary)
```

- Root `Grid` uses `DialogPadding`.
- Sizing: override `DialogWidth`/`DialogHeight` in code-behind only.
- Enter: `ConfirmCommand` on the dialog element.
- Esc: auto-wired `CloseDialogCommand` (override `CloseOnEscape` to disable).

> `ARCHITECTURE.md §7.2`, `UI_RULES.md §5.5 Standard Dialog Layout`

---

## 3  UX Standards

> Full specification: [`ARCHITECTURE.md` §5.6–5.9](ARCHITECTURE.md),
> [`UI_RULES.md` §9](UI_RULES.md),
> [`DEVELOPMENT_FLOW.md` §Regional](DEVELOPMENT_FLOW.md)

### 3.1  Keyboard navigation

| Key | Behavior |
|---|---|
| **Enter** | Walk editable inputs in tab order; on last field, execute `DefaultCommand` |
| **Shift+Enter** | Previous editable input |
| **Tab / Shift+Tab** | Standard WPF tab order |
| **Escape** | `EscapeCommand` → clear focus → `IsCancel` buttons (priority cascade) |

- Global behaviors via `KeyboardNav.cs` — never add per-window `PreviewKeyDown`.
- `DefaultCommand` and `EscapeCommand` are attached properties; nearest
  ancestor wins (inner form overrides dialog-level command).

> `ARCHITECTURE.md §5.6`

### 3.2  Keyboard shortcut map

**Global:** `Ctrl+D` Dashboard · `Ctrl+P` Products · `Ctrl+S` Sales ·
`Ctrl+L` Logout · `F5` Refresh

**Module-consistent:** `Ctrl+N` New · `Ctrl+E` Edit · `Delete` Delete ·
`Escape` Cancel · `Enter` Walk fields → submit on last

> `ARCHITECTURE.md §5.6 Keyboard shortcut map`

### 3.3  Auto-focus on load

- `AutoFocus.IsEnabled` is set globally on all Windows via the implicit
  Window style — first focusable input receives keyboard focus.
- Opt out per window: `h:AutoFocus.IsEnabled="False"`.
- Never write `Loaded` code-behind handlers for initial focus.

> `ARCHITECTURE.md §5.7`

### 3.4  Input UX behaviors

| Behavior | Activation | Effect |
|---|---|---|
| Smart cursor | Implicit TextBox style (global) | Text → select all; Numeric → cursor at end |
| Integer-only | `h:NumericInput.IsIntegerOnly="True"` | Block non-digit typing + paste |
| Decimal-only | `h:NumericInput.IsDecimalOnly="True"` | Allow digits + one decimal |

> `ARCHITECTURE.md §5.6 Input UX behaviors`

### 3.5  Inline validation

- Implicit styles set `Validation.ErrorTemplate` → red border + message.
- ViewModels extend `ObservableValidator` via `BaseViewModel`.
- Per-field: `[Required]` + `[NotifyDataErrorInfo]` attributes.
- Form-level: `Validate()` / `ErrorMessage` pattern.
- **Never** use `MessageBox` for validation feedback.

> `ARCHITECTURE.md §5.9`, `UI_RULES.md §6 Validation`

### 3.6  Status bar

`IStatusBarService` (singleton) — any ViewModel or service can post messages.

| Method | Use case |
|---|---|
| `Post(msg)` | Transient feedback (4 s auto-clear) |
| `Post(msg, duration)` | Custom auto-clear duration |
| `SetPersistent(msg)` | Page context (stays until replaced) |
| `Clear()` | Revert to "Ready" |

> `ARCHITECTURE.md §5.8`

### 3.7  Regional & culture

- Global culture: `en-IN` (Indian English, lakhs/crores formatting).
- All formatting through `IRegionalSettingsService` — never hardcode
  format strings.
- **Never** use `DateTime.Now` — use `regional.Now` (IST).
- `DateTime.UtcNow` only for DB-level comparisons (lockout expiry).

> `DEVELOPMENT_FLOW.md §Regional & Culture Rules`

---

## 4  Motion System

> Full specification: [`UI_RULES.md` §8](UI_RULES.md)

### 4.1  Motion tokens (`DesignSystem.xaml`)

| Token | Value | Usage |
|---|---|---|
| `FluentDurationFast` | 83 ms | Hover, instant feedback |
| `FluentDurationNormal` | 167 ms | Focus, panel reveal |
| `FluentDurationSlow` | 250 ms | View transitions, fades |
| `FluentEaseDecelerate` | CubicEase (EaseOut) | Entrances (content arrives fast, settles) |
| `FluentEaseAccelerate` | CubicEase (EaseIn) | Exits (content departs subtly) |
| `FluentEasePoint` | QuadraticEase (EaseInOut) | Scale / positional emphasis |

### 4.2  Attached behaviors (`h:Motion.*`)

| Property | Effect |
|---|---|
| `h:Motion.FadeIn="True"` | Opacity 0 → 1 (Slow, Decelerate) |
| `h:Motion.FadeOut="True"` | Opacity → 0 (Normal, Accelerate) |
| `h:Motion.ScaleHover="True"` | 0.985 → 1.0 on hover (Fast, Point) |
| `h:Motion.SlideFadeIn="True"` | Slide-up 12 px + fade (Slow, Decelerate) |

### 4.3  View transition (`ResponsiveContentControl`)

On content change: combined fade + slide-up (250 ms, Decelerate).
Scroll position resets to top. Non-blocking — input accepted immediately.

> `ARCHITECTURE.md §8`

### 4.4  Motion rules

| # | Rule |
|---|---|
| 1 | Never hardcode animation durations — use `FluentDuration*` tokens. |
| 2 | Never create inline Storyboards in views — use `MotionSystem.xaml` or `h:Motion.*`. |
| 3 | Entrances: `FluentEaseDecelerate`. Exits: `FluentEaseAccelerate`. |
| 4 | Hover effects: `FluentDurationFast` (83 ms) — must feel instant. |
| 5 | View transitions: `FluentDurationSlow` (250 ms) — must feel smooth. |
| 6 | Scale hover: `0.985 → 1.0` only. Never exceed 1.03. |
| 7 | Slide offsets: 12–20 px maximum. Never use large displacement. |

---

## 5  Smart Help System

> Implementation: `Core/Services/ContextHelpService.cs`,
> `Core/Services/TipRotationService.cs`,
> `Core/Helpers/SmartTooltip.cs`,
> `Core/Helpers/TipBannerAutoState.cs`,
> `Core/Controls/InlineTipBanner.cs`

### 5.1  Architecture overview

```
IContextHelpService              → context-aware help text (tooltips, overlays)
  ├── Rule pipeline              → ordered rules, first content match wins
  ├── Enrichment merge           → suffix-only rules (e.g. offline warnings) merged
  └── ExperienceLevelAdapter     → Beginner=detailed, Intermediate=default, Advanced=terse

ITipRotationService              → selects next InlineTipBanner tip per window
  ├── Experience-level gating    → TipLevel ceiling from UserExperienceProfile
  ├── Progressive frequency      → per-window cooldown (Beginner=0, Intermediate=5m, Advanced=30m)
  ├── Recency suppression        → ring buffer avoids repeating recent tips
  └── Priority weighting         → highest-priority non-recent tip wins

IOnboardingJourneyService        → tracks operator milestones, auto-promotes level
  ├── Beginner → Intermediate    → ≥5 distinct windows OR ≥3 sessions
  └── Intermediate → Advanced    → ≥5 billing completions OR ≥10 sessions
```

### 5.2  SmartTooltip (context-aware tooltips)

Attached properties on any UI element:

| Property | Purpose |
|---|---|
| `h:SmartTooltip.Text` | Simple tooltip text |
| `h:SmartTooltip.Header` | Bold header line |
| `h:SmartTooltip.Shortcut` | Keyboard shortcut (secondary text) |
| `h:SmartTooltip.UsageTip` | Usage tip (caption text) |
| `h:SmartTooltip.ContextKey` | Context-aware resolution key (resolves via `IContextHelpService`) |

**Timing model:** Cold delay 1.5 s → Warm delay 1.2 s → Display 5 s.
Anti-flicker: `IsMouseOver` check before open, generation counter, warm
state only on actual display.

### 5.3  InlineTipBanner (per-view guidance)

Compact banner below page title. One banner slot per view.

| Attached Property | Purpose |
|---|---|
| `h:TipBannerAutoState.TipKey` | Persists dismiss state to `ITipStateService` |
| `h:TipBannerAutoState.ContextKey` | Context-adaptive text via `IContextHelpService` |

**Placement rule:** Always Row 1 (below page title, above toolbar).
Never placed anywhere else.

**Dismiss behavior:** Fade-out → height collapse → persist via
`TipBannerAutoState.DismissFunc`. Restore by setting `IsDismissed = false`.

### 5.4  Experience-level adaptation

| Level | Tooltip text | Tip frequency | Tip level ceiling |
|---|---|---|---|
| **Beginner** | Detailed, step-by-step | Every visit (no cooldown) | `TipLevel.Beginner` only |
| **Intermediate** | Default rule output | Every 5 minutes per window | Up to `TipLevel.Normal` |
| **Advanced** | Terse, action-oriented | Every 30 minutes per window | All levels |

One-time tips (`IsOneTime = true`) that have never been shown bypass
the cooldown — they are too important to delay.

### 5.5  Onboarding tips

- Defined in `OnboardingTipDefinitions` (static catalog).
- Registered at startup by `OnboardingTipRegistrar`.
- Auto-dismissed when operator graduates past Beginner.
- Restored when journey is reset to Beginner.
- All use `TipLevel.Beginner`, `Priority = 90`, `IsOneTime = true`.

### 5.6  Adding new help content

| Task | Where |
|---|---|
| New context-aware help key | Add a rule to `ContextHelpService.Rules` pipeline |
| New beginner/advanced text | Add entry to `ExperienceLevelAdapter` dictionaries |
| New onboarding tip | Add to `OnboardingTipDefinitions.CreateAll()` + `AllTipIds` |
| New recurring tip | Register via `ITipRegistryService.Register()` in a module registrar |

---

## 6  Billing Safety Systems

> Implementation: `Modules/Billing/Services/`,
> `Core/Services/FocusLockService.cs`,
> `Core/Services/AppStateService.cs`

### 6.1  Safety rules

| # | Rule | Enforced by |
|---|---|---|
| 1 | **Active-session guard** — cannot exit Billing mode while `BillingSessionState.Active`. | `SmartBillingModeService` |
| 2 | **Payment lock** — all mode transitions blocked while payment is processing. Deferred stop flushed on `EndPaymentProcessingAsync`. | `SmartBillingModeService` |
| 3 | **Focus lock hold** — `IFocusLockService.HoldRelease()` prevents UI focus lock from releasing mid-payment. | `SmartBillingModeService` → `FocusLockService` |
| 4 | **Serialized transitions** — `SemaphoreSlim` ensures only one mode transition runs at a time. | `SmartBillingModeService` |
| 5 | **Auto-save** — cart state debounced to DB (1 s default), immediate flush on payment start. | `BillingAutoSaveService` |
| 6 | **Session recovery** — resumable billing sessions detected at login; user chooses Resume or Discard. | `IBillingResumeService` |
| 7 | **Stale session cleanup** — old active sessions archived before login loop. | `IStaleBillingSessionCleanupService` |

### 6.2  Auto-save lifecycle

```
CartChangedEvent  → debounce timer (1 s) → persist snapshot to DB
PaymentStartedEvent  → immediate flush (no debounce)
SessionCompleted / Cancelled  → cancel timer, mark row inactive
```

### 6.3  Financial transaction rules

- **Every** financial write (sales, payments, stock adjustments) must use
  a transaction boundary via one of:
  - `[Transactional]` attribute on pipeline commands (automatic wrapping).
  - `ITransactionSafetyService` (result-based — inspect `.Succeeded`).
  - `ITransactionHelper` (exception-based — callers catch on failure).
  - Manual `BeginTransactionAsync` + `CommitAsync` inside
    `CreateExecutionStrategy().ExecuteAsync()`.
- Work context created **inside** the retry lambda.
- Concurrency conflicts (`DbUpdateConcurrencyException`) caught and
  reported to the user.
- **Never** call `SaveChangesAsync()` outside a transaction for
  financial writes.

> `DEVELOPMENT_FLOW.md §Financial Transaction Rules`

### 6.4  Pricing rules

```
Product.SalePrice × Quantity
  → IPricingCalculationService.CalculateLineTotal()   [per-line]
  → LineTotal { Subtotal, TaxAmount, FinalAmount }

Σ line subtotals
  → IBillCalculationService.Calculate()               [whole bill]
  → BillSummary { Subtotal, DiscountAmount, TaxableAmount, TaxAmount, FinalAmount }
```

- Discounts are **bill-level only** — products carry zero discount logic.
- Discount applied **before tax** (Indian GST trade-discount, Section 15 CGST Act).
- Amount discounts capped at subtotal (never negative).

> `ARCHITECTURE.md §12`

---

## 7  Operational Modes

> Implementation: `Models/OperationalMode.cs`,
> `Core/Services/AppStateService.cs`,
> `Modules/Billing/Services/BillingModeService.cs`,
> `Core/Services/FocusLockService.cs`

### 7.1  Mode definitions

| Mode | Purpose | Feature set |
|---|---|---|
| **Management** | Back-office operations | Full: catalog, tax, users, reports, settings. Billing secondary. |
| **Billing** | Point-of-sale terminal | Streamlined: cart, payment, receipts. Management features hidden/read-only. |

### 7.2  Mode transitions

```
Management  ──[Start Billing]──▶  Billing
                                     │
                                     ├── Focus locked to billing module
                                     ├── Navigation restricted
                                     ├── Smart help adapts to billing context
                                     │
Billing     ──[Complete/Cancel]──▶  Management
                                     │
                                     ├── Focus lock released (unless held)
                                     ├── Full navigation restored
                                     └── Tips re-evaluate for management context
```

### 7.3  Focus lock rules

| State | Navigation | Triggered by |
|---|---|---|
| Unlocked | Free — all sidebar items accessible | Default (Management mode) |
| Locked | Restricted — billing module only | Entering Billing mode |
| Held | Locked + release deferred | Payment processing in progress |

- ViewModels read `IsFocusLockService.IsFocusLocked` to gate navigation commands.
- Only `SmartBillingModeService` and `FocusLockService` react to mode
  changes — no navigation gating logic in UI code.

### 7.4  Context-aware adaptation

When the operational mode changes, the following systems react
automatically via `IEventBus`:

| System | Event | Reaction |
|---|---|---|
| `ContextHelpService` | `OperationalModeChangedEvent` | Rebuilds `HelpContext`, re-evaluates rule pipeline |
| `TipRotationService` | (via `ContextHelpService`) | Invalidates tip cache, selects mode-appropriate tips |
| `FocusLockService` | `OperationalModeChangedEvent` | Activates/releases focus lock |
| `MainViewModel` | `OperationalModeChangedEvent` | Toggles sidebar visibility, toolbar composition |
| `OnboardingJourneyService` | `BillingSessionState.Completed` | Records billing completion toward promotion |

### 7.5  Offline mode interaction

When connectivity is lost (`OfflineModeChangedEvent`):

- `ContextHelpService` adds offline warning suffixes to all help text.
- `InlineTipBanner` shows offline-specific guidance.
- `SmartTooltip` suffixes tooltips with connectivity warnings.
- Billing continues with local auto-save — syncs on reconnect.

---

## Quick Compliance Checklist

Before submitting any change, verify:

### Architecture
- [ ] ViewModel inherits `BaseViewModel` (or `PinPadViewModel` for PIN entry)
- [ ] Dialog inherits `BaseDialogWindow`
- [ ] Content page uses `BasePage` as root element
- [ ] Module folder structure matches template
- [ ] Module depends only on `Core/`, `Models/`, `Data/`
- [ ] Cross-module communication uses `IEventBus` only
- [ ] New commands use `ICommandRequestHandler<,>` (pipeline-aware)
- [ ] Financial writes inside transaction (pipeline `[Transactional]`, safety service, or manual)

### UI
- [ ] Zero inline colors, margins, font sizes, corner radii
- [ ] Every `Foreground`/`Background`/`BorderBrush` uses `{StaticResource Fluent…}`
- [ ] Buttons use named style (`ToolbarButtonStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`)
- [ ] Sections wrapped in card styles (`FormCardStyle`, `SectionCardStyle`)
- [ ] Field widths use tokens (`FieldWidthCompact`/`Standard`/`Wide`)
- [ ] DataGrid uses `EnterpriseDataGridStyle` for primary data tables
- [ ] At least one DataGrid column uses `Width="*"`

### Layout
- [ ] Page root: `Margin="{StaticResource PagePadding}"` with one `Height="*"` row
- [ ] Dialog root: `Margin="{StaticResource DialogPadding}"`, three-row grid
- [ ] No `ScrollViewer` wrapping an entire window or dialog
- [ ] No fixed `Width`/`Height` on windows — sizing in code-behind only

### UX
- [ ] No `MessageBox` for validation — inline feedback only
- [ ] No per-window `PreviewKeyDown` — use `KeyboardNav` attached properties
- [ ] No code-behind `Loaded` for focus — use `AutoFocus.IsEnabled`
- [ ] Numeric inputs use `h:NumericInput` attached properties
- [ ] Error/success feedback uses `ErrorMessageStyle`/`SuccessMessageStyle`

### Motion
- [ ] No inline Storyboards — use `MotionSystem.xaml` or `h:Motion.*`
- [ ] No hardcoded durations — use `FluentDuration*` tokens
- [ ] Entrances: Decelerate. Exits: Accelerate.

### Help System
- [ ] Tip banner in Row 1 (below title, above toolbar) with `TipKey` + `ContextKey`
- [ ] New help keys have rules in the pipeline + adapter entries for Beginner/Advanced
- [ ] Onboarding tips are `IsOneTime = true`, `Level = Beginner`, `Priority = 90`

### Regional
- [ ] No `DateTime.Now` — use `IRegionalSettingsService.Now`
- [ ] No hardcoded format strings — use the regional service
- [ ] Currency/number formatting via `IRegionalSettingsService`
