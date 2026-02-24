# StoreAssistantPro — UI Rules

> Mandatory rules for every XAML view, window, and control template.
> Copilot and all contributors must follow these rules to maintain
> visual consistency across the enterprise WPF application.

---

## 1. Design System Is the Single Source of Truth

All visual values come from `Core/Styles/DesignSystem.xaml`.

| Rule | Example |
|---|---|
| **No inline colors** | ❌ `Foreground="#616161"` · ✅ `Foreground="{StaticResource FluentTextSecondary}"` |
| **No inline margins/padding** | ❌ `Margin="0,0,0,12"` · ✅ `Margin="{StaticResource FieldGroupSpacing}"` |
| **No inline font sizes** | ❌ `FontSize="13"` · ✅ `FontSize="{StaticResource FontSizeBody}"` |
| **No inline font families** | ❌ `FontFamily="Segoe UI"` · ✅ inherited from `FluentWindowStyle` |
| **No inline corner radii** | ❌ `CornerRadius="8"` · ✅ `CornerRadius="{StaticResource FluentCornerMedium}"` |
| **No inline shadows** | ❌ `<DropShadowEffect …/>` · ✅ `Effect="{StaticResource FluentShadowSmall}"` |
| **No inline widths for form fields** | ❌ `Width="80"` · ✅ `Width="{StaticResource FieldWidthCompact}"` |

If a needed token does not exist, add it to `DesignSystem.xaml` — never
hard-code the value at the call site.

---

## 2. Style Architecture

Styles are loaded in strict order by `App.xaml`:

```
DesignSystem.xaml   → pure tokens (colors, spacing, sizing, typography, motion)
  ↓
FluentTheme.xaml    → keyed control templates consuming tokens
  ↓
MotionSystem.xaml   → reusable Storyboards + motion styles (h:Motion behaviors)
  ↓
GlobalStyles.xaml   → implicit styles + named app styles (BasedOn Fluent)
  ↓
PosStyles.xaml      → POS-specific templates (keypad, role buttons)
```

### Rules

| # | Rule |
|---|---|
| 1 | **Never define tokens outside `DesignSystem.xaml`.** |
| 2 | **Never define ControlTemplates outside `FluentTheme.xaml` or `PosStyles.xaml`.** |
| 3 | **Named app styles (e.g. `PrimaryButtonStyle`) live in `GlobalStyles.xaml` and must use `BasedOn` to inherit a Fluent base.** |
| 4 | **Views must not define styles inline** — use `Style="{StaticResource …}"` references only. |
| 5 | **Use `StaticResource`, not `DynamicResource`**, unless the value genuinely changes at runtime (theme switching). |

---

## 3. Modern Light Theme

### 3.1  Named styles — never raw controls

Every control must use its implicit or named style. Do not override
properties that the style already provides.

#### Buttons

| Intent | Style Key | Base |
|---|---|---|
| Toolbar / compact action | `ToolbarButtonStyle` | `FluentSubtleButtonStyle` |
| Primary action (Save, Submit) | `PrimaryButtonStyle` | `FluentAccentButtonStyle` |
| Secondary action (Close, Cancel) | `SecondaryButtonStyle` | `FluentStandardButtonStyle` |
| POS keypad digit | `PosKeypadButtonStyle` | custom template |
| POS role selector | `SelectableUserButtonStyle` | custom template |

#### Typography

| Intent | Style Key |
|---|---|
| Page title (24 px bold) | `PageTitleStyle` |
| Dialog title (20 px bold) | `DialogTitleStyle` |
| Section header (14 px semi-bold) | `SectionHeaderStyle` |
| Stacked field label | `FieldLabelStyle` |
| Inline form-row label | `FormRowLabelStyle` |
| Muted caption | `CaptionLabelStyle` |
| Error feedback | `ErrorMessageStyle` |
| Success feedback | `SuccessMessageStyle` |

#### Containers

| Intent | Style Key | Target Type |
|---|---|---|
| Inline add/edit form | `FormCardStyle` | `Border` |
| Section panel (list, detail) | `SectionCardStyle` | `Border` |
| Section card header bar | `SectionCardHeaderStyle` | `Border` |
| Detail strip (bottom pane) | `DetailPanelStyle` | `Border` |
| Sidebar nav column | `SidebarNavStyle` | `Border` |
| Dashboard KPI tile | `StatCardStyle` | `Border` |
| Loading overlay | `OverlayCardStyle` | `Border` |
| Elevated popup container | `FluentCardElevatedStyle` | `Border` |

#### Data Grids

| Intent | Style Key |
|---|---|
| Default (basic virtualisation) | implicit `DataGrid` style |
| Primary data table (full optimisation) | `EnterpriseDataGridStyle` |

### 3.2  Card styles for sections

Group related content inside a `Border` with the appropriate card style.

```xml
<!-- ✅ Inline form wrapped in FormCardStyle -->
<Border Style="{StaticResource FormCardStyle}"
        Visibility="{Binding IsFormVisible, Converter={StaticResource BoolToVisibility}}">
    <StackPanel>
        <TextBlock Text="New Item" Style="{StaticResource SectionHeaderStyle}"/>
        <!-- fields -->
    </StackPanel>
</Border>

<!-- ✅ Section panel wrapped in SectionCardStyle -->
<Border Style="{StaticResource SectionCardStyle}">
    <Grid>
        <Border Style="{StaticResource SectionCardHeaderStyle}">
            <TextBlock Text="Category" FontWeight="SemiBold"/>
        </Border>
        <!-- content -->
    </Grid>
</Border>
```

| # | Rule |
|---|---|
| 1 | Every collapsible inline form must use `FormCardStyle`. |
| 2 | Every panel section (list, detail, sidebar) must use `SectionCardStyle` or `SidebarNavStyle`. |
| 3 | Dashboard KPI tiles must use `StatCardStyle` with a `FluentKpi*` background. |
| 4 | Never set `Background`, `CornerRadius`, `Effect`, or `BorderBrush` directly on a section `Border` — the card style handles it. |
| 5 | Override `Background` only for semantic highlighting (e.g., `FluentSuccessBackground` on edit forms). |

### 3.3  Color semantics

Never pick arbitrary colors. Use the semantic palette from `DesignSystem.xaml`:

| Semantic Role | Token |
|---|---|
| Primary text | `FluentTextPrimary` |
| Secondary / muted text | `FluentTextSecondary` |
| Placeholder / hint text | `FluentTextTertiary` |
| Disabled text | `FluentTextDisabled` |
| Text on accent background | `FluentTextOnAccent` |
| Page background | `FluentBackgroundPrimary` |
| Card / panel surface | `FluentSurface` |
| Elevated surface | `FluentSurfaceElevated` |
| Default border | `FluentStrokeDefault` |
| Focus ring | `FluentStrokeFocus` |
| Error border | `FluentStrokeError` |
| Brand accent | `FluentAccentDefault` / `Hover` / `Pressed` |
| Success | `FluentSuccess` / `FluentSuccessBackground` |
| Warning | `FluentWarning` / `FluentWarningBackground` |
| Error | `FluentError` / `FluentErrorBackground` |

#### KPI palette (dashboard tiles only)

`FluentKpiGreen` / `FluentKpiGreenMuted`, `FluentKpiOrange` / `FluentKpiOrangeMuted`,
`FluentKpiBlue` / `FluentKpiBlueMuted`, `FluentKpiPurple` / `FluentKpiPurpleMuted`.

---

## 4. Form Density Standards

This is a POS/enterprise application. Controls must be compact and
information-dense without feeling cramped.

### 4.1  Control heights

| Token | Value | Applies To |
|---|---|---|
| `ControlHeight` | 32 px | TextBox, ComboBox, DatePicker (`MinHeight`) |
| `ButtonHeight` | 34 px | Toolbar / compact buttons |
| `ButtonHeightLarge` | 36 px | Primary / secondary action buttons |
| `DataGridRowHeight` | 32 px | `EnterpriseDataGridStyle` fixed row height |

### 4.2  Control padding

| Token | Value | Use for |
|---|---|---|
| `ControlPadding` | `6` | Generic control internal padding |
| `ButtonPadding` | `12,6` | Standard toolbar button padding |
| `ButtonPaddingLarge` | `16,8` | Primary/secondary action button padding |
| `PagingButtonPadding` | `8,4` | Compact paging navigation buttons |

### 4.3  Field widths

| Token | Value | Use For |
|---|---|---|
| `FieldWidthCompact` | 80 px | Quantity, short codes, small numerics |
| `FieldWidthStandard` | 160 px | Names, single-line text, ComboBoxes |
| `FieldWidthWide` | 250 px | Search boxes, file paths, descriptions |

### 4.4  DataGrid column widths

| Token | Value | Use for |
|---|---|---|
| `ColumnWidthId` | `60` | ID / short key columns |
| `ColumnWidthPrice` | `110` | Currency / price columns |
| `ColumnWidthQty` | `60` | Quantity columns |
| `ColumnWidthStatus` | `100` | Status / type / enum columns |
| `ColumnWidthDate` | `120` | Date / DateTime columns |

> At least one column should use `Width="*"` (typically Name)
> to fill remaining horizontal space.

### 4.5  Spacing tokens

All spacing lives in `Core/Styles/DesignSystem.xaml`.

#### Container padding

| Token | Value | Use for |
|---|---|---|
| `PagePadding` | `20` (uniform) | Root `Grid` of every UserControl page |
| `DialogPadding` | `24` (uniform) | Root `Grid` of every BaseDialogWindow |
| `CardPadding` | `16` (uniform) | Card/section content area |
| `PanelPadding` | `12` (uniform) | Side panels, compact chrome regions |

#### Vertical spacing (bottom margin)

| Token | Value | Use for |
|---|---|---|
| `SectionSpacing` | `0,0,0,16` | Between major sections of a page |
| `TitleSpacing` | `0,0,0,16` | Below page/dialog title (built into `PageTitleStyle`) |
| `FieldGroupSpacing` | `0,0,0,12` | Below a label+control group in stacked forms |
| `ToolbarSpacing` | `0,0,0,12` | Below a toolbar/filter row |
| `ItemSpacing` | `0,0,0,8` | Between stacked items (list rows, message blocks) |
| `CompactItemSpacing` | `0,0,0,6` | Tight vertical gap inside dense forms |
| `FieldLabelSpacing` | `0,0,0,4` | Below a field label, above its control (built into `FieldLabelStyle`) |

#### Horizontal spacing (right margin)

| Token | Value | Use for |
|---|---|---|
| `FormColumnGap` | `0,0,12,0` | Right margin on labels in a two-column form |
| `WrapFieldGap` | `0,0,12,0` | Between fields in a horizontal `WrapPanel` form |
| `InlineControlSpacing` | `0,0,8,0` | Between inline controls (buttons, pickers) in a row |
| `InlineLabelGap` | `0,0,4,0` | Between a short label ("Qty:") and its inline control |

#### Top spacing (top margin)

| Token | Value | Use for |
|---|---|---|
| `FormActionBarSpacing` | `0,12,0,0` | Above the Save/Cancel action bar |
| `MessageBarSpacing` | `0,8,0,0` | Above error/success message blocks |

#### Uniform gap

| Token | Value | Use for |
|---|---|---|
| `PanelGap` | `12` (uniform) | Between side-by-side panels or split pane regions |

#### Grid spacing

| Token | Type | Value | Use for |
|---|---|---|---|
| `FormRowSpacing` | `GridLength` | `8` | Spacer rows in two-column form `Grid.RowDefinitions` |

#### Spacing scale (raw doubles)

| Token | Value | When to use |
|---|---|---|
| `SpacingXs` | `4` | Only when building a new `Thickness` not covered above |
| `SpacingSm` | `8` | " |
| `SpacingMd` | `12` | " |
| `SpacingLg` | `16` | " |
| `SpacingXl` | `20` | " |
| `SpacingXxl` | `24` | " |

### 4.6  Density rules

| # | Rule |
|---|---|
| 1 | Every TextBox, ComboBox, and DatePicker must have `MinHeight="{StaticResource ControlHeight}"` (handled by implicit style — do not remove). |
| 2 | Use `FieldWidthCompact` / `FieldWidthStandard` / `FieldWidthWide` for field `Width` — never raw numbers. |
| 3 | Use `ColumnWidthId` / `ColumnWidthPrice` / `ColumnWidthQty` / `ColumnWidthStatus` / `ColumnWidthDate` for DataGrid column widths. At least one column must use `Width="*"`. |
| 4 | Button padding comes from the style — never set `Padding` on a button that has a named style. |
| 5 | Toolbar buttons use `ToolbarButtonStyle` which includes `Margin="0,0,6,0"`. The last button in a row should set `Margin="0"` to remove trailing space. |

---

## 5. Layout Standards

### 5.1  Core principles

| # | Rule | Rationale |
|---|---|---|
| 1 | **`Grid` is the primary layout container** | Provides row/column star sizing for proportional scaling |
| 2 | **Use star (`*`) sizing for expandable regions** | Content fills available space without hard-coded dimensions |
| 3 | **Use `Auto` only for fixed-height chrome** | Headers, toolbars, status bars, and action rows that size to content |
| 4 | **Avoid fixed `Width`/`Height` on containers** | Fixed values clip content on high-DPI or small screens |
| 5 | **Set `MinWidth`/`MinHeight` on critical controls** | Prevents collapse to zero when star-sized space is constrained |
| 6 | **`TextWrapping="Wrap"` on all user-facing text** | Prevents truncation when text exceeds container width |
| 7 | **Let `BaseDialogWindow` and `WindowSizingService` control window dimensions** | Never set `Width`, `Height`, or `ResizeMode` in XAML |

**Panel selection:**

- Reserve `StackPanel` for short, single-axis sequences (a few buttons,
  a label + control pair). Never nest `StackPanel` as the sole child
  of a resizable area.
- Use `WrapPanel` for toolbars that may exceed available width.
- Use `DockPanel` for header/footer chrome patterns only.
- Use `UniformGrid` only for fixed-count equal-sized cells (dashboard cards).

### 5.2  Row/column sizing reference

```
Auto   → Row/column shrinks to fit content.  Use for headers, labels, buttons.
*      → Row/column shares remaining space.  Use for scrollable/data areas.
2*     → Weighted star: gets twice the share of a 1* sibling.
```

Every page layout must include **exactly one `*`-sized row** (and/or
column where appropriate) so the primary content region stretches to
fill the parent.

✅ **Correct — data area fills remaining space:**

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>   <!-- Page header -->
    <RowDefinition Height="Auto"/>   <!-- Toolbar / filters -->
    <RowDefinition Height="*"/>      <!-- DataGrid / ScrollViewer -->
    <RowDefinition Height="Auto"/>   <!-- Status / action bar -->
</Grid.RowDefinitions>
```

❌ **Wrong — every row is `Auto`, content may overflow or leave dead space:**

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
</Grid.RowDefinitions>
```

### 5.3  Window structure

| Window Type | Base Class | Root Padding | Sizing |
|---|---|---|---|
| Main shell | `Window` | none (chrome rows) | `WindowSizingService.ConfigureMainWindow` |
| Login / setup | `Window` | `DialogPadding` | `WindowSizingService.ConfigureStartupWindow` |
| Modal dialog | `BaseDialogWindow` | `DialogPadding` | `DialogWidth` / `DialogHeight` overrides |

- All windows inherit `FluentWindowStyle` automatically (background, font,
  text rendering, pixel snapping).
- Never set `Width`, `Height`, `ResizeMode`, or `WindowStartupLocation` in XAML.
- Dialogs must inherit `BaseDialogWindow` and use `ConfirmCommand` for Enter key.

### 5.4  Standard page layout

This is the template for any new navigable page registered via `NavigationPageRegistry`.

```
Row 0  Auto   Page title          (PageTitleStyle)
Row 1  Auto   Tip banner          (InlineTipBanner — see §8)
Row 2  Auto   Toolbar / filters   (ToolbarSpacing)
Row 3  Auto   Inline form         (FormCardStyle, collapsible)
Row 4  Auto   Error/success       (ErrorMessageStyle / SuccessMessageStyle)
Row 5  *      Primary data area   (DataGrid or ScrollViewer)
```

```xml
<UserControl x:Class="StoreAssistantPro.Modules.{Module}.Views.{Name}View"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:h="clr-namespace:StoreAssistantPro.Core.Helpers"
             xmlns:controls="clr-namespace:StoreAssistantPro.Core.Controls"
             mc:Ignorable="d"
             d:DesignHeight="550" d:DesignWidth="800">
    <Grid Margin="{StaticResource PagePadding}">
        <Grid.RowDefinitions>
            <!-- Row 0: Page title — fixed height, sizes to text -->
            <RowDefinition Height="Auto"/>
            <!-- Row 1: Tip banner — closable, persisted, context-adaptive -->
            <RowDefinition Height="Auto"/>
            <!-- Row 2: Toolbar / filters — fixed, sizes to buttons -->
            <RowDefinition Height="Auto"/>
            <!-- Row 3: Inline form (collapsible) — fixed when visible -->
            <RowDefinition Height="Auto"/>
            <!-- Row 4: Validation messages — fixed -->
            <RowDefinition Height="Auto"/>
            <!-- Row 5: Primary data area — STAR, fills remaining space -->
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Row 0: Page Header -->
        <TextBlock Text="Page Title" Style="{StaticResource PageTitleStyle}"/>

        <!-- Row 1: Tip Banner (see §8 Tip System Rules) -->
        <controls:InlineTipBanner Grid.Row="1"
            h:TipBannerAutoState.TipKey="{Module}.PageTip"
            h:TipBannerAutoState.ContextKey="CONTEXTKEY"
            Title="Quick tip"
            TipText="Tip description here."/>

        <!-- Row 2: Toolbar -->
        <StackPanel Grid.Row="2" Orientation="Horizontal"
                    Margin="{StaticResource ToolbarSpacing}">
            <Button Content="&#x2795; Add" Command="{Binding AddCommand}"
                    Style="{StaticResource ToolbarButtonStyle}"/>
            <Button Content="&#x270F;&#xFE0F; Edit" Command="{Binding EditCommand}"
                    Style="{StaticResource ToolbarButtonStyle}"/>
            <Button Content="&#x1F5D1; Delete" Command="{Binding DeleteCommand}"
                    Style="{StaticResource ToolbarButtonStyle}" Margin="0"/>
        </StackPanel>

        <!-- Row 3: Collapsible inline form -->
        <Border Grid.Row="3" Style="{StaticResource FormCardStyle}"
                Visibility="{Binding IsFormVisible,
                    Converter={StaticResource BoolToVisibility}}">
            <WrapPanel>
                <StackPanel Margin="{StaticResource WrapFieldGap}">
                    <TextBlock Text="Field One" Style="{StaticResource FieldLabelStyle}"/>
                    <TextBox Text="{Binding FieldOne, UpdateSourceTrigger=PropertyChanged}"
                             Width="{StaticResource FieldWidthStandard}"/>
                </StackPanel>
                <StackPanel Margin="{StaticResource WrapFieldGap}">
                    <TextBlock Text="Field Two" Style="{StaticResource FieldLabelStyle}"/>
                    <TextBox Text="{Binding FieldTwo, UpdateSourceTrigger=PropertyChanged}"
                             Width="{StaticResource FieldWidthCompact}"/>
                </StackPanel>
                <StackPanel VerticalAlignment="Bottom" Orientation="Horizontal">
                    <Button Content="&#x1F4BE; Save" Command="{Binding SaveCommand}"
                            Style="{StaticResource ToolbarButtonStyle}"/>
                    <Button Content="Cancel" Command="{Binding CancelCommand}"
                            Style="{StaticResource ToolbarButtonStyle}" Margin="0"/>
                </StackPanel>
            </WrapPanel>
        </Border>

        <!-- Row 4: Validation -->
        <TextBlock Grid.Row="4"
                   Text="{Binding ErrorMessage}"
                   Style="{StaticResource ErrorMessageStyle}"/>

        <!-- Row 5: Primary Data (star-sized) -->
        <DataGrid Grid.Row="5"
                  Style="{StaticResource EnterpriseDataGridStyle}"
                  ItemsSource="{Binding Items}"
                  SelectedItem="{Binding SelectedItem}">
            <DataGrid.Columns>
                <DataGridTextColumn Header="ID" Binding="{Binding Id}"
                                    Width="{StaticResource ColumnWidthId}"/>
                <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*"/>
                <DataGridTextColumn Header="Value" Binding="{Binding Value, StringFormat=C}"
                                    Width="{StaticResource ColumnWidthPrice}"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
```

### 5.5  Standard dialog layout

Every dialog inherits `BaseDialogWindow`. Sizing is controlled by
`DialogWidth`/`DialogHeight` overrides and `WindowSizingService`.

```
Row 0  Auto   Title               (DialogTitleStyle)
Row 1  *      Form body           (fields + messages)
Row 2  Auto   Action buttons      (right-aligned)
```

```xml
<core:BaseDialogWindow
        x:Class="StoreAssistantPro.Modules.{Module}.Views.{Name}Window"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:core="clr-namespace:StoreAssistantPro.Core"
        Title="{Name}">
    <Grid Margin="{StaticResource DialogPadding}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>   <!-- Dialog title -->
            <RowDefinition Height="*"/>      <!-- Form fields -->
            <RowDefinition Height="Auto"/>   <!-- Action buttons -->
        </Grid.RowDefinitions>

        <!-- Row 0: Title -->
        <TextBlock Text="Dialog Title" Style="{StaticResource DialogTitleStyle}"/>

        <!-- Row 1: Form body -->
        <StackPanel Grid.Row="1">
            <Grid Margin="{StaticResource FieldGroupSpacing}">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="Field Label *"
                           Style="{StaticResource FieldLabelStyle}"/>
                <TextBox Grid.Row="1"
                         Text="{Binding FieldValue, UpdateSourceTrigger=PropertyChanged}"
                         MaxLength="200"/>
            </Grid>

            <TextBlock Text="{Binding ErrorMessage}" Style="{StaticResource ErrorMessageStyle}"/>
            <TextBlock Text="{Binding SuccessMessage}" Style="{StaticResource SuccessMessageStyle}"/>
        </StackPanel>

        <!-- Row 2: Actions — pinned to bottom -->
        <StackPanel Grid.Row="2" Orientation="Horizontal"
                    HorizontalAlignment="Right"
                    Margin="{StaticResource FormActionBarSpacing}">
            <Button Content="💾 Save" Command="{Binding SaveCommand}"
                    Style="{StaticResource PrimaryButtonStyle}"
                    Margin="{StaticResource InlineControlSpacing}"/>
            <Button Content="Close" IsCancel="True"
                    Style="{StaticResource SecondaryButtonStyle}"/>
        </StackPanel>
    </Grid>
</core:BaseDialogWindow>
```

- The `*` row absorbs remaining space — fields sit at top, buttons pinned to bottom.
- Error/success messages inside the `*` region — multi-line errors won't push buttons off-screen.
- Primary button: `PrimaryButtonStyle`. Secondary (Close/Cancel): `SecondaryButtonStyle`.

### 5.6  Grid column patterns

#### Two-column form (label + input)

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" MinWidth="80"/>  <!-- Labels -->
        <ColumnDefinition Width="*"/>                    <!-- Inputs stretch -->
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="{StaticResource FormRowSpacing}"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <TextBlock Grid.Row="0" Grid.Column="0" Text="Name"
               Style="{StaticResource FormRowLabelStyle}"/>
    <TextBox   Grid.Row="0" Grid.Column="1"
               Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"/>

    <TextBlock Grid.Row="2" Grid.Column="0" Text="Price"
               Style="{StaticResource FormRowLabelStyle}"/>
    <TextBox   Grid.Row="2" Grid.Column="1"
               Text="{Binding Price, UpdateSourceTrigger=PropertyChanged}"/>
</Grid>
```

**Rules:**
- Labels use `FormRowLabelStyle` (font size, vertical alignment, right margin via `FormColumnGap`).
- Spacer rows use `FormRowSpacing` (`GridLength`, 8 px) — never use `Margin` between rows.
- Action bar uses `FormActionBarSpacing` (`Thickness`, 12 px top) and `InlineControlSpacing` between buttons.
- Primary button: `PrimaryButtonStyle`. Secondary: `SecondaryButtonStyle`.

#### Multi-column data entry (equal weight)

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>    <!-- Column A -->
        <ColumnDefinition Width="*"/>    <!-- Column B -->
        <ColumnDefinition Width="Auto"/> <!-- Action buttons -->
    </Grid.ColumnDefinitions>
</Grid>
```

#### Content area (MainWindow)

```xml
<!-- Primary content fills entire workspace -->
<controls:ResponsiveContentControl Grid.Row="2"
    Content="{Binding CurrentView}"/>
```

### 5.7  MinWidth / MinHeight safety net

Apply `MinWidth` and `MinHeight` to controls that must remain functional even when
their star-sized container is squeezed.

| Control | Recommended Minimum |
|---|---|
| `TextBox` (single-line input) | `MinWidth="80"` |
| `TextBox` (multiline) | `MinHeight="50"` |
| `DataGrid` | `MinHeight="100"` |
| `ComboBox` | `MinWidth="100"` |
| `Button` (action) | `MinWidth="60"` |
| Sidebar (`ColumnDefinition`) | `MinWidth="160"` |

### 5.8  Anti-patterns

#### ❌ All-Auto rows with no star row

```xml
<!-- BAD -->
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
</Grid.RowDefinitions>
```

**Fix:** Change the primary data row to `Height="*"`.

#### ❌ Fixed pixel heights on data containers

```xml
<!-- BAD — clips content on high-DPI, wastes space on large screens -->
<DataGrid Height="400" ... />
```

**Fix:** Place the `DataGrid` in a star-sized row and remove the `Height`.

#### ❌ Deeply nested StackPanels for page structure

```xml
<!-- BAD — outer StackPanel never clips or scrolls -->
<StackPanel>
    <TextBlock Text="Header"/>
    <DataGrid ItemsSource="{Binding Items}"/>
    <StackPanel Orientation="Horizontal">...</StackPanel>
</StackPanel>
```

**Fix:** Use `Grid` with star rows for the outer structure. Reserve `StackPanel`
for small, local groupings (button rows, label+input pairs).

#### ❌ Setting sizing properties on BaseDialogWindow in XAML

```xml
<!-- BAD — conflicts with WindowSizingService -->
<core:BaseDialogWindow Width="500" Height="400" ResizeMode="CanResize" ...>
```

**Fix:** Override `DialogWidth` / `DialogHeight` in code-behind only.

---

## 6. Scroll Policy

> **Rule:** `ScrollViewer` must never wrap an entire Window, Dialog, or
> UserControl root. Scroll is only permitted around data-driven content
> that can grow beyond its container.

### ✅ Allowed

| Context | Why |
|---|---|
| `DataGrid` / `ListView` / `ListBox` in a `*`-sized row | Dynamic row count; grid scrolls internally |
| `ItemsControl` bound to a collection | Dynamic item count |
| Dynamic content pane (`ContentControl` in settings) | Content varies per tab |
| `TextBox` with `AcceptsReturn="True"` + `VerticalScrollBarVisibility="Auto"` | Internal text overflow |
| `ResponsiveContentControl` (main content area) | Shell-level host; `ViewportConstrainedPanel` keeps star rows working |

### ❌ Disallowed

| Context | Correct approach |
|---|---|
| Login / PIN screen | Fixed `Grid` — keypad fills `*`-row |
| First-time setup dialog | Fixed `Grid` — all fields fit within sized window |
| Firm / User management dialogs | 3-row `Grid`: `Auto` title → `*` form → `Auto` buttons |
| Settings window outer shell | Split-panel: sidebar nav + `ScrollViewer` on content pane only |
| Dashboard page | `UniformGrid` cards in `*`-row — never scrolls |

### Development-time enforcement

`LayoutDiagnostics` (attached behavior in `Core/Helpers/LayoutDiagnostics.cs`)
runs **DEBUG-only** on every `Window.Loaded` event and writes warnings to
the Visual Studio **Output → Debug** pane when it detects:

- `ScrollViewer` as the `Window.Content` (wraps entire window).
- `ScrollViewer` as a root-panel child spanning all rows.
- `ScrollViewer` whose subtree contains only form controls (no
  `DataGrid`/`ListView`/`ItemsControl`).

Activated globally via `GlobalStyles.xaml`:

```xml
<Setter Property="h:LayoutDiagnostics.IsEnabled" Value="True"/>
```

Opt-out per window: `h:LayoutDiagnostics.IsEnabled="False"`.

### Compliance audit

| File | ScrollViewer | Status |
|---|---|---|
| `UnifiedLoginWindow.xaml` | None | ✅ |
| `FirstTimeSetupWindow.xaml` | None | ✅ |
| `FirmManagementWindow.xaml` | None | ✅ |
| `UserManagementWindow.xaml` | None | ✅ |
| `TaxManagementWindow.xaml` | None | ✅ |
| `TasksWindow.xaml` | None | ✅ |
| `ResumeBillingDialog.xaml` | None | ✅ |
| `MainWorkspaceView.xaml` | None | ✅ |
| `MainWindow.xaml` | None | ✅ |
| `GeneralSettingsView.xaml` | None | ✅ |
| `SecuritySettingsView.xaml` | None | ✅ |
| `BackupSettingsView.xaml` | None | ✅ |
| `AppInfoView.xaml` | None | ✅ |
| `ProductsView.xaml` | Line 142: wraps DataGrid + inline forms | ✅ Data area |
| `SalesView.xaml` | Line 77: cart DataGrid; Line 162: sale detail | ✅ Data area |
| `SystemSettingsWindow.xaml` | Line 65: dynamic settings content pane | ✅ Content pane |

---

## 7. Motion Guidelines

All animations use the centralized motion system in `Core/Styles/MotionSystem.xaml`
and `Core/Helpers/Motion.cs`. Never create ad-hoc Storyboards in views.

### 7.1  Motion tokens (`DesignSystem.xaml`)

| Token | Type | Value | Usage |
|---|---|---|---|
| `FluentDurationFast` | Duration | 83 ms | Hover, instant feedback |
| `FluentDurationNormal` | Duration | 167 ms | Focus, panel reveal |
| `FluentDurationSlow` | Duration | 250 ms | View transitions, fades |
| `FluentEaseDecelerate` | CubicEase (EaseOut) | — | Entrances (content arrives fast, settles) |
| `FluentEaseAccelerate` | CubicEase (EaseIn) | — | Exits (content departs subtly) |
| `FluentEasePoint` | QuadraticEase (EaseInOut) | — | Scale / positional emphasis |
| `MotionScaleHoverFrom` | Double | 0.985 | Resting scale for hover effect |
| `MotionSlideOffsetSmall` | Double | 12 px | Slide distance for entrances |
| `MotionSlideOffsetNormal` | Double | 20 px | Slide distance for larger panels |

### 7.2  Attached behaviors (`h:Motion.*`)

Use in any view XAML — no code-behind needed:

| Behavior | Attached Property | Effect |
|---|---|---|
| Fade in on load | `h:Motion.FadeIn="True"` | Opacity 0 → 1 (Slow, Decelerate) |
| Fade out on unload | `h:Motion.FadeOut="True"` | Opacity → 0 (Normal, Accelerate) |
| Scale hover | `h:Motion.ScaleHover="True"` | 0.985 → 1.0 on hover (Fast, Point) |
| Slide + fade in | `h:Motion.SlideFadeIn="True"` | Slide-up 12 px + fade (Slow, Decelerate) |

### 7.3  Reusable storyboards (`MotionSystem.xaml`)

For ControlTemplate / Style triggers:

| Storyboard Key | Effect |
|---|---|
| `MotionFadeIn` | Opacity 0 → 1 (Slow) |
| `MotionFadeOut` | Opacity → 0 (Normal) |
| `MotionFadeInFast` | Opacity 0 → 1 (Fast) |
| `MotionFadeOutFast` | Opacity → 0 (Fast) |
| `MotionScaleHoverEnter` | Scale → 1.0 (requires named `HoverScale` transform) |
| `MotionScaleHoverLeave` | Scale → 0.985 (requires named `HoverScale` transform) |
| `MotionSlideFadeIn` | Slide-up + fade in (requires named `SlideTransform`) |
| `MotionSlideFadeOut` | Slide-down + fade out (requires named `SlideTransform`) |

### 7.4  Ready-made styles

| Style Key | Applies |
|---|---|
| `MotionFadeInStyle` | Fade in on Loaded |
| `MotionSlideFadeInStyle` | Slide + fade in on Loaded |
| `MotionScaleHoverStyle` | Subtle scale pulse on hover |
| `MotionCardStyle` | Fade in + scale hover (for cards/tiles) |

### 7.5  Motion rules

| # | Rule |
|---|---|
| 1 | Never hardcode animation durations — use `FluentDuration*` tokens. |
| 2 | Never create inline Storyboards in views — use `MotionSystem.xaml` storyboards or `h:Motion.*` behaviors. |
| 3 | Entrances use `FluentEaseDecelerate` (EaseOut). Exits use `FluentEaseAccelerate` (EaseIn). |
| 4 | Hover effects use `FluentDurationFast` (83 ms) — must feel instant. |
| 5 | View transitions use `FluentDurationSlow` (250 ms) — must feel smooth. |
| 6 | Scale hover is `0.985 → 1.0` — barely perceptible, professional. Never exceed 1.03. |
| 7 | Slide offsets are 12–20 px maximum. Never use large displacement. |

---

## 8. Tip System Rules

Every content page includes an `InlineTipBanner` in **Row 1** (below title,
above toolbar). This is the standard slot — tips are never placed anywhere else.

### Placement

```xml
<!-- Row 1: Tip Banner (standard slot) -->
<controls:InlineTipBanner Grid.Row="1"
    h:TipBannerAutoState.TipKey="{Module}.PageTip"
    h:TipBannerAutoState.ContextKey="CONTEXTKEY"
    Title="Quick tip"
    TipText="Tip description here."/>
```

### Attached properties

| Property | Purpose |
|---|---|
| `h:TipBannerAutoState.TipKey` | Persists dismiss state to `ITipStateService` — once dismissed, stays dismissed across sessions |
| `h:TipBannerAutoState.ContextKey` | Context-adaptive text resolution via `IContextHelpService` — text updates automatically when mode/connectivity/experience level changes |

### Behavior

- Collapses to zero height when dismissed — no wasted space.
- Dismiss persisted across sessions via `TipKey`.
- Context-adaptive text via `ContextKey` — refreshes on mode, connectivity,
  and experience-level changes.
- Dismiss animation: fade-out → height collapse.
- Restore: setting `IsDismissed = false` reverses the animation.

### Rules

| # | Rule |
|---|---|
| 1 | Every content page must include an `InlineTipBanner` in Row 1. |
| 2 | `TipKey` format: `"{Module}.{TipName}"` (e.g. `"Products.PageTip"`). |
| 3 | `ContextKey` maps to a key in the `IContextHelpService` rule pipeline. |
| 4 | Never place tips anywhere other than Row 1. |
| 5 | Never manage tip dismiss state manually — `TipBannerAutoState` handles it. |

---

## 9. Desktop UX Standards

### Keyboard & focus

| Behavior | How |
|---|---|
| Enter = confirm | `ConfirmCommand` on `BaseDialogWindow`, or `h:KeyboardNav.DefaultCommand` on a container |
| Escape = cancel | Auto-wired by `BaseDialogWindow`, or `h:KeyboardNav.EscapeCommand` on forms |
| Auto-focus first input | Global via `h:AutoFocus.IsEnabled` on Window style |
| Numeric-only TextBox | `h:NumericInput.IsIntegerOnly="True"` or `h:NumericInput.IsDecimalOnly="True"` |
| Select-all on focus | Implicit TextBox style sets `h:SelectOnFocus.IsEnabled="True"` |

### Validation

- All input controls use `InlineValidationErrorTemplate` (set by implicit styles).
- Error messages display via `ErrorMessageStyle` bound to ViewModel `ErrorMessage`.
- Success messages display via `SuccessMessageStyle` bound to ViewModel `SuccessMessage`.
- Never use `MessageBox` for validation — use inline feedback only.

### Data display

| Rule | Detail |
|---|---|
| Use `EnterpriseDataGridStyle` for primary data grids | Enables full virtualisation, fixed row height, read-only mode |
| Fixed column widths via tokens | `ColumnWidthId`, `ColumnWidthPrice`, `ColumnWidthQty`, etc. |
| One `Width="*"` column required | Typically the Name column — fills remaining space |
| Loading indicator | `"⏳ Loading..."` TextBlock in the same Grid row, centered |
| Empty state | Consider showing a centered message when `ItemsSource` is empty |

### Navigation

- Page navigation uses `ContentControl` with `DataTemplate` matching by ViewModel type.
- All `DataTemplate` mappings are registered in `App.xaml`.
- Settings uses sidebar `ListBox` + `ContentControl` pattern (`SystemSettingsWindow`).

---

## 10. Attached Behaviors

Use built-in helpers instead of writing code-behind for common interactions.
All behaviors live in `Core/Helpers/`.

### Keyboard & focus

| Behavior | Attached Property | Activation |
|---|---|---|
| Enter = execute command | `h:KeyboardNav.DefaultCommand` | Per-container |
| Escape = execute command | `h:KeyboardNav.EscapeCommand` | Per-container |
| Auto-focus first input | `h:AutoFocus.IsEnabled` | Implicit Window style (global) |
| Select text on focus | `h:SelectOnFocus.IsEnabled` | Implicit TextBox style (global) |

### Input constraints

| Behavior | Attached Property | Activation |
|---|---|---|
| Integer-only input | `h:NumericInput.IsIntegerOnly` | Per-control |
| Decimal-only input | `h:NumericInput.IsDecimalOnly` | Per-control |
| Placeholder text | `h:Watermark.Text` | Per-control |

### Help & guidance

| Behavior | Attached Property | Activation |
|---|---|---|
| Context-aware tooltip | `h:SmartTooltip.Text` / `.Header` / `.Shortcut` / `.UsageTip` / `.ContextKey` | Per-element |
| Contextual hint icon | `h:HelpHint.Text` / `.ContextKey` | Per-element |
| Tip banner auto-state | `h:TipBannerAutoState.TipKey` / `.ContextKey` | Per `InlineTipBanner` |

### Billing-specific

| Behavior | Attached Property | Activation |
|---|---|---|
| Dim non-billing content | `h:BillingDimBehavior.IsEnabled` | Per-container |
| Focus enforcement | `h:BillingFocusBehavior.IsEnabled` | Per-container |

### Animation & transitions

| Behavior | Attached Property | Activation |
|---|---|---|
| Fade in on load | `h:Motion.FadeIn` | Per-element |
| Fade out on unload | `h:Motion.FadeOut` | Per-element |
| Scale hover | `h:Motion.ScaleHover` | Per-element |
| Slide + fade in | `h:Motion.SlideFadeIn` | Per-element |
| Status pill transition | `h:StatusPillTransition.IsEnabled` | Per status-pill element |
| Auto-dismiss | `h:AutoDismiss.Duration` | Per-element |
| Notification badge | `h:NotificationBadgeBehavior.Count` | Per bell icon |

### Development-time diagnostics (DEBUG only)

| Behavior | Attached Property | Activation |
|---|---|---|
| Layout compliance | `h:LayoutDiagnostics.IsEnabled` | Implicit Window style (global) |
| Style compliance | `h:StyleComplianceDiagnostics.IsEnabled` | Implicit Window style (global) |

---

## 11. Converters

Only use converters registered in `App.xaml`:

| Converter | Key | Usage |
|---|---|---|
| `BooleanToVisibilityConverter` | `BoolToVisibility` | `Visibility="{Binding IsVisible, Converter={StaticResource BoolToVisibility}}"` |
| `InverseBoolConverter` | `InverseBoolConverter` | `IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBoolConverter}}"` |
| `EqualityConverter` | `EqualityConverter` | Multi-binding equality checks |
| `PinDotConverter` | `PinDotConverter` | PIN display masking |
| `BoolToActiveTextConverter` | `BoolToActiveText` | Active/Inactive status labels |

Never create a one-off converter in a view. If a new converter is needed,
add it to `Core/Helpers/` and register it in `App.xaml`.

---

## 12. Prohibited Patterns

| ❌ Do Not | ✅ Instead |
|---|---|
| `Background="#FFFFFF"` | `Background="{StaticResource FluentSurface}"` |
| `Margin="0,0,0,12"` | `Margin="{StaticResource FieldGroupSpacing}"` |
| `FontSize="13"` | `FontSize="{StaticResource FontSizeBody}"` or inherit from style |
| `CornerRadius="8"` | `CornerRadius="{StaticResource FluentCornerMedium}"` |
| `<DropShadowEffect BlurRadius="8" …/>` | `Effect="{StaticResource FluentShadowSmall}"` |
| `Width="80"` on a TextBox | `Width="{StaticResource FieldWidthCompact}"` |
| `Width="60"` on a DataGrid column | `Width="{StaticResource ColumnWidthId}"` |
| `Style="{StaticResource {x:Type Button}}"` | `Style="{StaticResource ToolbarButtonStyle}"` (pick the right named style) |
| `<Style TargetType="Button">` in a view | Use a named style from `GlobalStyles.xaml` |
| `Foreground="Red"` | `Foreground="{StaticResource FluentError}"` |
| `MessageBox.Show(…)` for validation | Bind to `ErrorMessage` + `ErrorMessageStyle` |
| `ScrollViewer` wrapping entire window | `ScrollViewer` only around data-driven content (§6) |
| Setting `Width`/`Height` on `BaseDialogWindow` | Override `DialogWidth`/`DialogHeight` in code-behind |
| `DynamicResource` for design tokens | `StaticResource` (tokens do not change at runtime) |
| Inline `<Style>` in view XAML | Named style in `GlobalStyles.xaml` with `BasedOn` |
| New converter defined inside a view | Add to `Core/Helpers/` and register in `App.xaml` |
| `DataGrid Height="400"` | Place in a `*`-sized row and remove the `Height` |
| Nested `StackPanel` as page structure | Use `Grid` with `Auto` + `*` rows |

---

## Quick Compliance Checklist

Before submitting any XAML change, verify:

**Design system**
- [ ] Zero inline colors — every `Foreground`, `Background`, `BorderBrush` uses `{StaticResource Fluent…}`
- [ ] Zero inline margins — every `Margin` and `Padding` uses a spacing token
- [ ] Zero inline font sizes — uses type scale token or inherits from style

**Styles**
- [ ] Buttons use a named style (`ToolbarButtonStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`)
- [ ] Section content wrapped in a card style (`FormCardStyle`, `SectionCardStyle`)

**Form density**
- [ ] Field widths use `FieldWidthCompact` / `FieldWidthStandard` / `FieldWidthWide`
- [ ] DataGrid columns use `ColumnWidth*` tokens; at least one column is `Width="*"`
- [ ] DataGrid uses `EnterpriseDataGridStyle` for primary data tables
- [ ] Numeric inputs use `h:NumericInput.IsIntegerOnly` or `h:NumericInput.IsDecimalOnly`

**Layout**
- [ ] Root container is `Grid` (not `StackPanel` or `DockPanel`)
- [ ] Exactly one row uses `Height="*"` for the primary data area
- [ ] Page root: `Margin="{StaticResource PagePadding}"`
- [ ] Dialog root: `Margin="{StaticResource DialogPadding}"`, inherits `BaseDialogWindow`
- [ ] `MinWidth`/`MinHeight` set on inputs and data controls
- [ ] `TextWrapping="Wrap"` on user-facing text blocks
- [ ] Inline forms use `WrapPanel` or `Grid` — not deeply nested `StackPanel`

**Scroll**
- [ ] No `ScrollViewer` wrapping an entire window or dialog
- [ ] `ScrollViewer` only wraps `DataGrid`, dynamic item lists, or report content

**Tips**
- [ ] `InlineTipBanner` in Row 1 with `TipKey` and `ContextKey`

**UX**
- [ ] Error/success feedback uses `ErrorMessageStyle` / `SuccessMessageStyle`
- [ ] No `MessageBox` for validation feedback
- [ ] Dialogs use `ConfirmCommand` for Enter key
