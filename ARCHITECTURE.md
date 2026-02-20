# Architecture – Store Assistant Pro

> **.NET 10 · WPF · MVVM · CommunityToolkit.Mvvm · Entity Framework Core**
>
> This document is the single source of truth for project structure,
> coding rules, and UI standards. Every contributor and every AI agent
> must follow these rules when adding or modifying code.

---

## 1  Solution layout

```
StoreAssistantPro/
├── Core/                         # Shared infrastructure (no module dependencies)
│   ├── Base/                     #   BaseViewModel, BasePage, BaseDialogWindow, BaseCommand
│   ├── Commands/                 #   ICommandBus, ICommandHandler, CommandResult
│   ├── Controls/                 #   ResponsiveContentControl, ViewportConstrainedPanel
│   ├── Events/                   #   IEventBus, IEvent
│   ├── Features/                 #   FeatureFlags, IFeatureToggleService
│   ├── Helpers/                  #   Converters, InputValidator, PinHasher, NumericInput
│   ├── Navigation/               #   INavigationService, NavigationPageRegistry
│   ├── Services/                 #   WindowSizingService, DialogService, AppStateService …
│   ├── Session/                  #   ISessionService, SessionService
│   ├── Styles/                   #   GlobalStyles.xaml (spacing, typography, named styles)
│   └── Workflows/                #   IWorkflow, WorkflowManager, WorkflowStep, StepResult
├── Data/                         # EF Core DbContext + Migrations
├── Models/                       # Domain entities (Product, Sale, SaleItem, UserCredential …)
├── Modules/                      # Vertical feature slices
│   ├── Authentication/           #   Login, first-time setup, user selection
│   ├── Billing/                  #   Billing workflows
│   ├── Firm/                     #   Firm management dialog
│   ├── MainShell/                #   MainWindow, DashboardView, shell services
│   ├── Products/                 #   Product CRUD
│   ├── Sales/                    #   Sale entry + history
│   ├── Startup/                  #   App bootstrap workflow
│   ├── SystemSettings/           #   Settings window + category views
│   └── Users/                    #   User management dialog
├── App.xaml / App.xaml.cs        # Resource dictionaries, DataTemplates, startup
├── HostingExtensions.cs          # DI registration helpers
├── app.manifest                  # PerMonitorV2 DPI awareness
└── StoreAssistantPro.Tests/      # Unit / integration tests
```

---

## 2  Module structure

Each module is a self-contained vertical slice:

```
Modules/<ModuleName>/
├── Commands/       # ICommand + ICommandHandler pairs
├── Events/         # Module-specific IEvent types
├── Services/       # Module services (interface + implementation)
├── ViewModels/     # ViewModels (derive from BaseViewModel)
├── Views/          # XAML views (derive from BasePage or BaseDialogWindow)
├── Workflows/      # Multi-step flows (optional)
└── <ModuleName>Module.cs   # DI registration + page mapping
```

**Rules:**

- A module may only depend on `Core/`, `Models/`, and `Data/`.
- Modules must never reference another module directly.
- Cross-module communication uses `IEventBus` (publish/subscribe).
- Each module exposes a single `Add<Name>Module()` extension method
  called from `HostingExtensions`.

---

## 3  MVVM wiring

| Concern | Mechanism |
|---|---|
| ViewModel → View | Implicit `DataTemplate` in `App.xaml` |
| Page navigation | `INavigationService.NavigateTo<TViewModel>()` |
| Business actions | `ICommandBus.SendAsync<TCommand>(command)` |
| Cross-module events | `IEventBus.PublishAsync<TEvent>(event)` |
| Multi-step flows | `IWorkflowManager.StartAsync(workflowName)` |
| Feature gating | `IFeatureToggleService.IsEnabled(FeatureFlags.X)` |

---

## 4  Base classes

### `BaseViewModel`

Every ViewModel must inherit `BaseViewModel`. It provides:

- `IsBusy` / `IsLoading` — busy-state tracking.
- `ErrorMessage` — first-failure validation display.
- `Validate(builder => ...)` — fluent rule-chain validation.
- `RunAsync(Func<Task>)` — guarded async execution with error capture.
- `Title` — auto-derived from the class name.

### `BasePage`

Every new content page must use `BasePage` as its root XAML element.
It provides:

- `PageTitle` — rendered in the title bar row.
- `HeaderContent` — optional toolbar controls beside the title.
- Error-message bar — auto-bound to `BaseViewModel.ErrorMessage`.
- Loading overlay — auto-bound to `BaseViewModel.IsLoading`.
- Standard 20 px page padding.
- A `*`-sized content row that fills remaining space.

```xml
<core:BasePage x:Class="StoreAssistantPro.Modules.Foo.Views.FooView"
               xmlns:core="clr-namespace:StoreAssistantPro.Core"
               PageTitle="Foo">
    <!-- page content -->
</core:BasePage>
```

### `BaseDialogWindow`

Every modal dialog must inherit `BaseDialogWindow`.
Sizing is set in code-behind only — never in XAML:

```csharp
protected override double DialogWidth  => 500;
protected override double DialogHeight => 400;
```

---

## 5  UI standards

### 5.1  Grid-first layout

- **Always use `Grid`** as the primary layout panel.
- Reserve `StackPanel` for short, single-axis sequences (a few buttons,
  a label + control pair). Never nest `StackPanel` as the sole child
  of a resizable area — it gives children infinite extent and prevents
  proper resize.
- Use `WrapPanel` for toolbars that may exceed available width.
- Use `DockPanel` for header/footer chrome patterns only.
- Use `UniformGrid` only for fixed-count equal-sized cells (dashboard cards).

### 5.2  Star sizing mandatory

Every page layout must include **exactly one `*`-sized row** (and/or
column where appropriate) so the primary content region stretches to
fill the parent.

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>   <!-- title / toolbar -->
    <RowDefinition Height="Auto"/>   <!-- form fields -->
    <RowDefinition Height="*"/>      <!-- primary data (DataGrid, list, etc.) -->
</Grid.RowDefinitions>
```

### 5.3  ScrollViewer for content areas

- **Main content pages** are hosted in `ResponsiveContentControl`, which
  provides a `ScrollViewer` + `ViewportConstrainedPanel`. Star-sized
  rows work correctly and scrollbars appear only when content exceeds
  the viewport.
- **Dialog windows** must wrap their content `Grid` in a
  `ScrollViewer VerticalScrollBarVisibility="Auto"` so that tight
  layouts never clip.
- **Settings / sub-window content areas** must also wrap the
  `ContentControl` in a `ScrollViewer`.

### 5.4  No fixed sizes unless required

| Allowed | Forbidden |
|---|---|
| `MaxWidth` / `MaxHeight` on parent `StackPanel` to cap field groups | `Width` + `MaxWidth` set to the same value (redundant, prevents shrink) |
| `MaxHeight` on inline DataGrids (cart, user list) | `Height` on DataGrids inside constrained dialogs |
| Fixed `Width` on narrow input fields (PIN, quantity) | `Width` on full-width TextBox / ComboBox |

**Pattern for constrained form fields:**

```xml
<!-- Width constraint lives on the parent, child stretches within it -->
<StackPanel MaxWidth="400" HorizontalAlignment="Left"
            Margin="{StaticResource FieldGroupSpacing}">
    <TextBlock Text="Firm Name *" Style="{StaticResource FieldLabelStyle}"/>
    <TextBox Text="{Binding FirmName}" MaxLength="200"/>
</StackPanel>
```

### 5.5  DPI-aware application

- The application manifest (`app.manifest`) declares
  **PerMonitorV2** DPI awareness.
- `WindowSizingService` sizes the main window to 90 % of
  `SystemParameters.WorkArea` and re-centres on display changes.
- **Never use pixel-based positioning** (`Canvas.Left`, `Margin` for
  alignment) — use layout panels, `HorizontalAlignment`, and
  `VerticalAlignment`.
- All font sizes, spacing, and padding use the token system in
  `GlobalStyles.xaml` which scales naturally at any DPI.

### 5.6  Keyboard navigation

Defined in `Core/Helpers/KeyboardNav.cs` and activated globally via an
implicit `Window` style in `GlobalStyles.xaml`.

#### Enter-key focus flow

Enter is resolved in priority order — the first match wins:

| Priority | Condition | Action |
|---|---|---|
| 0 | `AcceptsReturn` TextBox, Button, open ComboBox | Native behavior (newline, click, select) |
| 1 | `Shift` held | Move focus to previous editable input |
| 2 | Next editable input exists | Move focus to it (skip disabled / read-only) |
| 3 | No editable input ahead + `DefaultCommand` bound, `CanExecute` true | Execute the command (submit) |
| 4 | Fallback | Move focus to next focusable element (button, etc.) |

**Editable inputs** that participate in Enter-key navigation:
`TextBox` (not read-only), `PasswordBox`, `ComboBox`, `DatePicker`.

**Automatically skipped:** disabled controls, `IsReadOnly` TextBoxes,
buttons, text blocks, data grids, and any non-input element.

This gives the natural data-entry flow:

```
Name [TextBox]  →Enter→  Price [TextBox]  →Enter→  Qty [TextBox]  →Enter→  Submit
```

**`DefaultCommand` is an attached property** set on any container element.
The behavior walks up the visual tree from the focused control and
finds the first command.  It only executes after the user has navigated
past the last editable field — **no accidental submits**.

```xml
<!-- Enter walks Name→Price→Qty, then submits on the last one -->
<Grid h:KeyboardNav.DefaultCommand="{Binding SaveCommand}">
    <TextBox Text="{Binding Name}"/>     <!-- Enter → next -->
    <TextBox Text="{Binding Price}"/>    <!-- Enter → next -->
    <TextBox Text="{Binding Qty}"/>      <!-- Enter → submit -->
    <Button Content="Save" Command="{Binding SaveCommand}"/>
</Grid>
```

Bind the **same command** used on the primary action button.  The
command's `CanExecute` guards both the button and the Enter key.

| Key combo | Action |
|---|---|
| **Enter** | Next editable input; if none, execute `DefaultCommand` |
| **Shift+Enter** | Previous editable input |
| **Tab / Shift+Tab** | Standard WPF tab-order navigation |

**Automatic exclusions** (Enter keeps its native meaning):

- `TextBox` with `AcceptsReturn="True"` — Enter inserts a newline.
- `ButtonBase` — Enter invokes the button's own command / click.
- `ComboBox` with drop-down open — Enter selects the highlighted item.

#### Escape-key behavior (tiered)

ESC is resolved in priority order — the first match wins:

| Priority | Condition | Action |
|---|---|---|
| 1 | `EscapeCommand` bound on nearest ancestor | Execute the command |
| 2 | Input control (`TextBox` / `PasswordBox`) focused | Clear focus |
| 3 | None of the above | Don't handle — `IsCancel` buttons work |

**`EscapeCommand` is an attached property** set on any container element.
The behavior walks up the visual tree from the focused control and
executes the first command it finds. This means:

- An **inner form** can override the **dialog-level** close command.
- A **dialog** can override the **window-level** default.

```xml
<!-- Dialog-level: ESC closes (BaseDialogWindow sets this automatically) -->
<core:BaseDialogWindow ...>

    <!-- Form-level: ESC cancels the form (overrides dialog close) -->
    <Border h:KeyboardNav.EscapeCommand="{Binding CancelFormCommand}">
        <!-- form fields -->
    </Border>
</core:BaseDialogWindow>
```

| Window type | Default ESC behavior | Mechanism |
|---|---|---|
| `BaseDialogWindow` | Closes with `DialogResult = false` | Auto-wired `CloseDialogCommand` |
| `MainWindow` | Clears input focus (no command) | Tier 2 fallback |
| Auth windows | Falls through to `IsCancel` buttons | Tier 3 fallback |

To disable auto-close on a specific dialog, override `CloseOnEscape`:

```csharp
protected override bool CloseOnEscape => false;
```

#### Rules

- Tab order follows the WPF visual tree by default. Set `TabIndex` on
  controls only when the visual layout doesn't match the desired
  navigation path (e.g. multi-column forms).
- To disable on a specific container:
  `h:KeyboardNav.IsEnabled="False"`.
- Never add per-window `PreviewKeyDown` handlers for focus, submit, or
  escape logic — the global behavior handles it.
- Never add code-behind `Loaded` handlers to set initial focus — use
  `AutoFocus.IsEnabled` instead.

#### Keyboard shortcut map

**Global (MainWindow — always active):**

| Shortcut | Action |
|---|---|
| `Ctrl+D` | Navigate to Dashboard |
| `Ctrl+P` | Navigate to Products |
| `Ctrl+S` | Navigate to Sales |
| `Ctrl+L` | Logout |
| `F5` | Refresh current view |
| `Alt` | Activate menu bar (WPF built-in) |

**Products page:**

| Shortcut | Action |
|---|---|
| `Ctrl+N` | Open Add Product form |
| `Ctrl+E` | Open Edit Product form |
| `Delete` | Delete selected product |
| `Enter` | Walk fields → save on last field |
| `Escape` | Cancel / close inline form |

**Sales page:**

| Shortcut | Action |
|---|---|
| `Ctrl+N` | Open New Sale form |
| `Enter` | Walk fields → add to cart on last field |
| `Escape` | Cancel / close sale form |

**Dialogs (BaseDialogWindow):**

| Shortcut | Action |
|---|---|
| `Enter` | Walk fields → confirm on last field |
| `Escape` | Close dialog |

**Auth windows:**

| Shortcut | Action |
|---|---|
| `Enter` | Submit (Login / Save) |

**Shortcut rules for future modules (billing, reports):**

- Page-level shortcuts go in `UserControl.InputBindings`.
- Inline form `DefaultCommand` / `EscapeCommand` go on the form's
  container (`Border`, `Grid`) — *not* on the page root.
- Inner commands override outer commands (nearest ancestor wins).
- Use `Ctrl+N` for "New", `Ctrl+E` for "Edit", `Delete` for delete,
  `Escape` for cancel — consistent across all modules.

### 5.7  Auto-focus on load

Defined in `Core/Helpers/AutoFocus.cs` and activated globally via the
implicit `Window` style in `GlobalStyles.xaml`.

When a container with `AutoFocus.IsEnabled="True"` is loaded, the
first focusable input control receives keyboard focus automatically.
Focus target follows `TabIndex` ordering via
`FocusNavigationDirection.First`.

The global `Window` style enables this for every window.  To opt out:

```xml
<Window h:AutoFocus.IsEnabled="False">
```

Per-container activation is also supported for page-level or form-level
auto-focus (e.g. a settings UserControl hosted inside a tab):

```xml
<UserControl h:AutoFocus.IsEnabled="True">
```

### 5.8  Status bar

Defined in `Core/Services/StatusBarService.cs`.  The `IStatusBarService`
is a singleton that any ViewModel or service can inject to post messages.

| Method | Behavior |
|---|---|
| `Post(msg)` | Show message, auto-clear after 4 seconds |
| `Post(msg, duration)` | Show message, auto-clear after custom duration |
| `SetPersistent(msg)` | Show message until replaced or cleared |
| `Clear()` | Revert to `DefaultMessage` ("Ready") |

The XAML binds directly to the service instance exposed by MainViewModel:

```xml
<TextBlock Text="{Binding StatusBar.Message}"/>
```

**Rules:**

- Navigation commands use `SetPersistent` (page context stays visible).
- Transient actions (refresh, dialog close, event) use `Post` (auto-clear).
- Never set status text via direct property assignment — always use the
  service so auto-clear timers are managed correctly.

### 5.9  Inline validation feedback

Defined in `GlobalStyles.xaml` (`InlineValidationErrorTemplate`) and
`BaseViewModel` (`ObservableValidator` base class).

**Visual layer** — implicit styles on all input controls set
`Validation.ErrorTemplate` to a shared `ControlTemplate` that shows:

```
┌ red border (1.5 px) ─────────┐
│  [control]                    │
└───────────────────────────────┘
⚠ Error message text
```

**ViewModel layer** — `BaseViewModel` extends `ObservableValidator`
(CommunityToolkit MVVM), giving every ViewModel `INotifyDataErrorInfo`
support.  Opt in per property:

```csharp
[ObservableProperty]
[NotifyDataErrorInfo]
[Required(ErrorMessage = "Firm name is required.")]
public partial string FirmName { get; set; } = string.Empty;
```

Validation fires automatically when the property changes.  In the
submit command, call `ValidateAllProperties()` to check all at once:

```csharp
ValidateAllProperties();
if (HasErrors) return;
```

**Coexistence:** The existing `Validate()` / `ErrorMessage` pattern
for form-level messages continues to work alongside per-field errors.
Use `[NotifyDataErrorInfo]` for inline field feedback and `Validate()`
for cross-field or server-side error messages.

---

## 6  Spacing & typography system

Defined in `Core/Styles/GlobalStyles.xaml` and merged into `App.xaml`.

### 6.1  Spacing scale (4 px base unit)

| Token | Value | Usage |
|---|---|---|
| `SpacingXs` | 4 | Label → control gap |
| `SpacingSm` | 8 | Inline control gap |
| `SpacingMd` | 12 | Field-group bottom, toolbar gap |
| `SpacingLg` | 16 | Section separator, title margin |
| `SpacingXl` | 20 | Page padding |
| `SpacingXxl` | 24 | Dialog padding |

### 6.2  Pre-built `Thickness` resources

| Key | Value | Use for |
|---|---|---|
| `PagePadding` | `20` | Main content page outer margin |
| `DialogPadding` | `24` | Dialog window outer margin |
| `SectionSpacing` | `0,0,0,16` | Between major visual sections |
| `FieldGroupSpacing` | `0,0,0,12` | Between form-field groups |
| `FieldLabelSpacing` | `0,0,0,4` | Label → control gap |
| `ToolbarSpacing` | `0,0,0,12` | Below toolbars |
| `InlineControlSpacing` | `0,0,8,0` | Between inline toolbar items |
| `TitleSpacing` | `0,0,0,16` | Below page/dialog title |
| `ControlPadding` | `6` | TextBox / PasswordBox / ComboBox |
| `ButtonPadding` | `12,6` | Toolbar buttons |
| `ButtonPaddingLarge` | `16,8` | Primary action buttons |
| `CardPadding` | `16` | Inline form card |

### 6.3  Typography scale

| Key | Size | Role |
|---|---|---|
| `FontSizePageTitle` | 24 | Main content page titles |
| `FontSizeDialogTitle` | 20 | Dialog / settings panel titles |
| `FontSizeSectionHeader` | 16 | In-form section headers |
| `FontSizeBody` | 13 | Standard body text |
| `FontSizeLabel` | 12 | Field labels |
| `FontSizeCaption` | 11 | Captions, footnotes |

### 6.4  Named styles

| Key | Target | Purpose |
|---|---|---|
| `PageTitleStyle` | `TextBlock` | 24 px bold, title margin |
| `DialogTitleStyle` | `TextBlock` | 20 px bold, title margin |
| `SectionHeaderStyle` | `TextBlock` | 14 px semi-bold |
| `FieldLabelStyle` | `TextBlock` | 12 px semi-bold, 4 px bottom |
| `CaptionLabelStyle` | `TextBlock` | 12 px semi-bold, muted |
| `ToolbarButtonStyle` | `Button` | Compact padding + 6 px right gap |
| `PrimaryButtonStyle` | `Button` | Large padding, 13 px semi-bold |
| `FormCardStyle` | `Border` | Card background, radius, padding |
| `ErrorMessageStyle` | `TextBlock` | Red, wrapping |
| `SuccessMessageStyle` | `TextBlock` | Green, wrapping |

### 6.5  Implicit control styles

`TextBox`, `PasswordBox`, `ComboBox`, and `DatePicker` receive default
`Padding` and `VerticalContentAlignment` via implicit styles. Locally
set values on individual controls still take priority.

### 6.6  Input UX behaviors

Defined in `Core/Helpers/` and activated via implicit styles or
per-control attributes.

| Behavior | Class | Activation | Purpose |
|---|---|---|---|
| Smart cursor on focus | `SelectOnFocus` | Implicit `TextBox` style (global) | Text → select all; Numeric → cursor at end |
| Integer-only input | `NumericInput.IsIntegerOnly` | Per-control attribute | Block non-digit typing and paste |
| Decimal-only input | `NumericInput.IsDecimalOnly` | Per-control attribute | Allow digits and one decimal point |

**Smart cursor positioning** is global — every `TextBox` receives
focus-aware cursor behavior via Tab, Enter, or click.  The mode is
auto-detected from existing `NumericInput` attached properties:

| Field type | Detection | On focus |
|---|---|---|
| Text field | No `NumericInput` property | Select all text |
| Numeric field | `IsIntegerOnly` or `IsDecimalOnly` set | Cursor at end of text |
| Multi-line | `AcceptsReturn="True"` | Excluded (no action) |

To opt out:

```xml
<TextBox h:SelectOnFocus.IsEnabled="False"/>
```

**Numeric input** is opt-in per control:

```xml
<TextBox h:NumericInput.IsIntegerOnly="True"/>   <!-- PINs, quantities -->
<TextBox h:NumericInput.IsDecimalOnly="True"/>   <!-- prices, amounts  -->
```

Both modes block invalid characters on typing *and* paste.

---

## 7  Window sizing & dialog standard

### 7.1  Window sizing

| Window | Strategy |
|---|---|
| **MainWindow** | 90 % of `SystemParameters.WorkArea`, centred, `NoResize`. Re-sized on display change. |
| **BaseDialogWindow** | Fixed `DialogWidth` / `DialogHeight` in code-behind, `NoResize`, centred over owner. |
| **Startup windows** | Fixed size via `ConfigureStartupWindow()`, centred on screen, `NoResize`. |

All sizing is programmatic — `Width`, `Height`, `ResizeMode`, and
`WindowStartupLocation` must **never** be set in XAML.

### 7.2  BaseDialogWindow contract

Every modal dialog must inherit `BaseDialogWindow`.  The base class
provides the full enterprise dialog standard automatically:

| Behavior | Mechanism | Opt-out |
|---|---|---|
| Fixed size | `DialogWidth` / `DialogHeight` abstract properties | — (required) |
| No resize | `ResizeMode.NoResize` via `IWindowSizingService` | — |
| Centered over owner | `WindowStartupLocation.CenterOwner` | — |
| Modal | `ShowDialog()` via dialog service | — |
| Enter = confirm | `ConfirmCommand` DP → `KeyboardNav.DefaultCommand` | Don't bind |
| ESC = cancel | `CloseDialogCommand` → `DialogResult = false` | `CloseOnEscape => false` |
| Auto-focus | First input focused on load (global style) | `AutoFocus.IsEnabled="False"` |
| Keyboard nav | Enter/Tab traversal (global style) | `KeyboardNav.IsEnabled="False"` |

**Minimal dialog XAML:**

```xml
<core:BaseDialogWindow x:Class="…"
        xmlns:core="clr-namespace:StoreAssistantPro.Core"
        Title="Edit Item"
        ConfirmCommand="{Binding SaveCommand}">
    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="0">
    <Grid Margin="25">
        <!-- form fields -->
        <Button Content="Save" Command="{Binding SaveCommand}"/>
        <Button Content="Cancel" IsCancel="True"/>
    </Grid>
    </ScrollViewer>
</core:BaseDialogWindow>
```

**Minimal dialog code-behind:**

```csharp
public partial class EditItemWindow : BaseDialogWindow
{
    protected override double DialogWidth  => 450;
    protected override double DialogHeight => 350;

    public EditItemWindow(IWindowSizingService sizing, EditItemViewModel vm)
        : base(sizing)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
```

**Key rules:**

- `ConfirmCommand` must bind the **same command** as the primary action
  button.  The command's `CanExecute` guards both the button and Enter.
- Dialogs with multiple tabs / no single primary action (e.g.
  `SystemSettingsWindow`) omit `ConfirmCommand` — sub-views use
  `h:KeyboardNav.DefaultCommand` on their own containers instead.
- `IsCancel="True"` on the cancel button still works alongside the
  base class ESC handling (the base class handles ESC first; if
  `CloseOnEscape` is overridden to `false`, `IsCancel` buttons take
  over).

---

## 8  Responsive content hosting

The main content area uses a three-layer stack:

```
ResponsiveContentControl          ← stretches to fill, defines ScrollViewer
  └─ ScrollViewer                 ← vertical Auto, horizontal Disabled
       └─ ViewportConstrainedPanel  ← passes viewport size as finite constraint
            └─ <Page Content>       ← star-sized rows work correctly
```

- When content fits the viewport → no scrollbar, `*` rows fill space.
- When content exceeds the viewport → vertical scrollbar appears,
  content scrolls naturally.
- This eliminates the classic WPF problem of `*` rows collapsing
  inside a plain `ScrollViewer`.

---

## 9  Dependency injection

- **Composition root**: `HostingExtensions.cs` + `App.xaml.cs`.
- Each module registers itself via `Add<Name>Module()`.
- **Lifetimes**:
  - Services (state, session, settings): `Singleton`.
  - Command handlers: `Transient`.
  - ViewModels: `Transient`.
  - Views / Windows: `Transient`.
  - `DbContextFactory`: `Singleton`; individual `DbContext`: short-lived scoped usage.

---

## 10  Command / event bus

### Commands (one-to-one)

```
ViewModel  →  ICommandBus.SendAsync<SaveProductCommand>(cmd)
                  ↓
              ICommandHandler<SaveProductCommand>.HandleAsync(cmd)
                  ↓
              CommandResult (Success / Failure + message)
```

### Events (one-to-many)

```
Handler / Service  →  IEventBus.PublishAsync(new SaleCompletedEvent(...))
                          ↓
                      All subscribers of SaleCompletedEvent are called
```

Events are the **only** mechanism for cross-module communication.

---

## 11  Data access

- **EF Core** with `IDbContextFactory<AppDbContext>`.
- Services create short-lived `DbContext` instances via
  `ITransactionHelper` for unit-of-work scoping.
- Migrations live in `Data/Migrations/`.
- Connection string: `appsettings.json` → `ConnectionStrings:Default`.

---

## 12  Checklist for new features

1. Create the module folder under `Modules/<Name>/`.
2. Add `<Name>Module.cs` with `Add<Name>Module()` extension method.
3. Register in `HostingExtensions`.
4. ViewModels inherit `BaseViewModel`.
5. Content pages use `BasePage` as root element.
6. Dialog windows inherit `BaseDialogWindow`.
7. Set `ConfirmCommand="{Binding SaveCommand}"` on the dialog window
   element (binds Enter to the primary action).
8. Wrap dialog content in `ScrollViewer`.
8. Use `Grid` with `Auto` + `*` row definitions.
9. Use spacing tokens from `GlobalStyles.xaml` — no magic numbers.
10. Use `MaxWidth` on parent `StackPanel` for constrained fields — no
    fixed `Width` on the control itself.
11. Add implicit `DataTemplate` in `App.xaml` for ViewModel → View mapping.
12. Add commands via `ICommandBus`, events via `IEventBus`.
13. Gate visibility behind `IFeatureToggleService` if applicable.
14. Set `TabIndex` on form controls when the visual layout doesn't match
    the desired keyboard navigation order. Enter-key navigation is
    automatic — do not add per-window key handlers.
