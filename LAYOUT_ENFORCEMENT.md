# StoreAssistantPro — Layout Enforcement Rules

> **Purpose:** Prevent layout regressions across all content pages and
> dialog windows.  Every contributor and AI agent must follow these rules
> when creating or modifying any XAML view.
>
> **Canonical references:**
>
> | Document | Sections |
> |---|---|
> | [`UI_RULES.md`](UI_RULES.md) | §5 Layout Standards, §6 Scroll Policy |
> | [`ARCHITECTURE.md`](ARCHITECTURE.md) | §6 Design System, Layout pattern |
> | [`MASTER_RULES.md`](MASTER_RULES.md) | §2.5 Layout rules |

---

## 1  Mandatory Layout Container

### 1.1  Content pages

Every navigable content page hosted in `ResponsiveContentControl` must
use `EnterprisePageLayout` as its root element inside the `UserControl`:

```xml
<UserControl x:Class="StoreAssistantPro.Modules.{Module}.Views.{Name}View"
             xmlns:controls="clr-namespace:StoreAssistantPro.Core.Controls"
             xmlns:h="clr-namespace:StoreAssistantPro.Core.Helpers"
             ...>
    <controls:EnterprisePageLayout>
        <controls:EnterprisePageLayout.TipBannerContent>
            <!-- InlineTipBanner -->
        </controls:EnterprisePageLayout.TipBannerContent>
        <controls:EnterprisePageLayout.ToolbarContent>
            <!-- Toolbar card -->
        </controls:EnterprisePageLayout.ToolbarContent>

        <!-- Main content (DataGrid, primary data) — fills star row -->
        <Border Style="{StaticResource SectionCardStyle}" ClipToBounds="True">
            ...
        </Border>

        <controls:EnterprisePageLayout.BottomFormContent>
            <!-- Collapsible add/edit forms -->
        </controls:EnterprisePageLayout.BottomFormContent>
    </controls:EnterprisePageLayout>
</UserControl>
```

| # | Rule |
|---|---|
| 1 | **Do not** build a manual `Grid` with `RowDefinitions` at the page root — use `EnterprisePageLayout`. |
| 2 | **Do not** set `Margin="{StaticResource PagePadding}"` on the page root — the template provides it. |
| 3 | **Do not** manually add loading overlays or error/success message bars — the template provides them. |
| 4 | Content that goes in the star-sized row is placed as the `Content` of `EnterprisePageLayout`. |

### 1.2  Dialog windows

Dialogs inherit `BaseDialogWindow`.  Their internal layout uses a
3-row `Grid` (`Auto` title / `*` form body / `Auto` action bar) as
defined in `UI_RULES.md §5.5`.  They do not use `EnterprisePageLayout`
(dialogs have their own sizing via `DialogWidth`/`DialogHeight`).

### 1.3  Settings pages

Settings content panes hosted inside `SystemSettingsWindow` use the
sidebar + content split pattern.  Each settings view is a `UserControl`
with its own `ScrollViewer` around data-driven content only.

---

## 2  Star-Sized Main Content

| # | Rule |
|---|---|
| 1 | Every page must have **exactly one star-sized (`*`) row** for its primary data region. |
| 2 | `EnterprisePageLayout` enforces this automatically — `Content` goes into the `*` row (Row 2). |
| 3 | The `*` row must contain the primary `DataGrid`, `ListView`, or data display. |
| 4 | The `*` row has `MinHeight="100"` built into the template to prevent collapse. |
| 5 | **Never** place a `DataGrid` in an `Auto`-sized row — it will grow unbounded. |
| 6 | **Never** use `Height` on a `DataGrid` — star sizing handles it. |

### Verification

The star row is structurally guaranteed by `EnterprisePageLayout`.  If
a page does not use the template, it must have this exact pattern:

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>  <!-- chrome rows -->
    ...
    <RowDefinition Height="*"/>     <!-- exactly one star row -->
    ...
    <RowDefinition Height="Auto"/>  <!-- bottom chrome -->
</Grid.RowDefinitions>
```

---

## 3  Grid Row Enforcement

> `EnterprisePageLayout` handles the page-level row structure
> automatically.  These rules apply to **all grids** — the page
> template, dialog windows, form cards, and any nested `Grid` with
> `RowDefinitions`.

### 3.1  Row sizing decision tree

```
Is this row the primary data / workspace area?
 ├─ YES → Height="*"   (exactly ONE per grid hierarchy)
 └─ NO  → Height="Auto" (everything else)
```

There is **no** scenario in this application where a page or dialog
grid requires more than one star row.

### 3.2  What belongs in each row type

| Row type | `Height` | Contains | Example |
|---|---|---|---|
| **Page chrome** | `Auto` | Tip banner, toolbar, filter bar, message bar, status bar | `EnterprisePageLayout` rows 0, 1, 3, 4, 5 |
| **Primary workspace** | `*` | `DataGrid`, `ListView`, primary content area | `EnterprisePageLayout` row 2 |
| **Bottom form** | `Auto` | Collapsible add/edit forms, secondary actions | `EnterprisePageLayout` row 4 |
| **Dialog title** | `Auto` | `DialogTitleStyle` text | Dialog row 0 |
| **Dialog body** | `*` | Form fields + error/success messages | Dialog row 1 |
| **Dialog actions** | `Auto` | Save / Cancel buttons | Dialog row 2 |
| **Form spacer** | `FormRowSpacing` | Empty spacer between form field rows | `{StaticResource FormRowSpacing}` (8 px) |
| **Form field** | `Auto` | Label + input control pair | Form grid content rows |
| **Form actions** | `Auto` | Save / Cancel with `FormActionBarSpacing` top margin | Last row of form grid |

### 3.3  Rules

| # | Rule |
|---|---|
| 1 | Every `Grid` with `RowDefinitions` must have **at most one** `Height="*"` row. |
| 2 | The star row must contain the primary scrollable or expandable content. |
| 3 | All non-star rows must use `Height="Auto"` or `Height="{StaticResource FormRowSpacing}"`. |
| 4 | **Never** use fixed pixel heights on rows (`Height="200"`, `Height="400"`). |
| 5 | **Never** use weighted star rows (`2*`, `3*`) in page or dialog grids — split panes use column stars, not row stars. |
| 6 | **Never** use `Height="*"` on a bottom-form row — forms are bounded and must be `Auto`. |
| 7 | **Never** omit the star row — all-`Auto` grids leave dead space or overflow. |

### 3.4  Multiple star rows — why they are banned

Two or more `*` rows in the same `Grid` split remaining space
proportionally.  In this application that produces:

| Symptom | Cause |
|---|---|
| Half-empty DataGrid with unused space below | `*` form row below `*` data row |
| Form card floating in the middle of whitespace | `*` row stretching a bounded form |
| Inconsistent layouts across screen sizes | Proportional split changes with window height |
| DataGrid too short to be useful | Star budget split 50/50 with an empty region |

**Fix:** Only the primary data row gets `*`.  Everything else is `Auto`.

### 3.5  Correct patterns

#### ✅ Page (via EnterprisePageLayout — handled automatically)

```
Row 0  Auto    TipBanner
Row 1  Auto    Toolbar
Row 2  *       DataGrid / primary content     ← single star
Row 3  Auto    Messages
Row 4  Auto    BottomForm
Row 5  Auto    StatusBar
```

#### ✅ Dialog (BaseDialogWindow)

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>   <!-- Title          -->
    <RowDefinition Height="*"/>      <!-- Form body      -->  ← single star
    <RowDefinition Height="Auto"/>   <!-- Action buttons -->
</Grid.RowDefinitions>
```

#### ✅ Form card (inside BottomFormContent)

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>                            <!-- Header      -->
    <RowDefinition Height="{StaticResource FormRowSpacing}"/>
    <RowDefinition Height="Auto"/>                            <!-- Field row 1 -->
    <RowDefinition Height="{StaticResource FormRowSpacing}"/>
    <RowDefinition Height="Auto"/>                            <!-- Field row 2 -->
    ...
    <RowDefinition Height="Auto"/>                            <!-- Actions     -->
</Grid.RowDefinitions>
```

No star row — the form is entirely `Auto`-sized.  The parent
`BottomFormContent` slot (also `Auto`) collapses to fit.

### 3.6  Anti-patterns

#### ❌ Multiple star rows

```xml
<!-- BAD — splits space, DataGrid gets only half the window -->
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>     <!-- DataGrid -->
    <RowDefinition Height="*"/>     <!-- Form — gets 50% for no reason -->
</Grid.RowDefinitions>
```

**Fix:** Change the form row to `Auto`.

#### ❌ Star row on a form

```xml
<!-- BAD — form stretches into empty whitespace -->
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>   <!-- Header -->
    <RowDefinition Height="*"/>      <!-- Form fields — unbounded -->
    <RowDefinition Height="Auto"/>   <!-- Buttons -->
</Grid.RowDefinitions>
```

**Fix:** Use `Auto` for all form field rows with `FormRowSpacing`
spacers.  The star row pattern is only valid for **dialogs** (where
the `*` body anchors buttons to the bottom).

#### ❌ All-Auto page grid

```xml
<!-- BAD — no row absorbs remaining space; dead space or overflow -->
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
</Grid.RowDefinitions>
```

**Fix:** Use `EnterprisePageLayout` (star row built in), or change
the data row to `Height="*"`.

#### ❌ Fixed pixel row height

```xml
<!-- BAD — clips on small screens, wastes space on large screens -->
<RowDefinition Height="400"/>
```

**Fix:** Use `Auto` (bounded content) or `*` (expandable content).

---

## 4  No Floating Controls

| # | Rule |
|---|---|
| 1 | Every visible element must be inside a named layout slot (`TipBannerContent`, `ToolbarContent`, `Content`, `BottomFormContent`, `StatusBarContent`). |
| 2 | **No** `Canvas`, absolute positioning, `RenderTransform` for layout, or negative margins to position elements. |
| 3 | **No** elements placed outside the `Grid` hierarchy (e.g., overlays that are siblings of the root `Grid`). |
| 4 | Loading overlays are provided by the template — do not add manual `TextBlock` loading indicators. |
| 5 | Empty-state indicators (e.g., "No items found") overlay the `DataGrid` inside the same `Grid` cell using `Visibility` triggers — not floating. |

---

## 5  Card Layout for Sections

Every visual section must be wrapped in the appropriate card style.

| Section | Card Style | Rule |
|---|---|---|
| Toolbar / filter bar | `SectionCardStyle` + `SectionCardHeaderStyle` | Toolbar lives inside a card with a header bar. |
| Primary data area | `SectionCardStyle` with `ClipToBounds="True"` | DataGrid card clips to rounded corners. |
| Collapsible add/edit form | `FormCardStyle` | Each form is its own card with padding and shadow. |
| Detail / summary panel | `DetailPanelStyle` | Bottom split panes use the detail style. |
| Dashboard KPI tiles | `StatCardStyle` | Individual stat cards, not raw `Border` elements. |

| # | Rule |
|---|---|
| 1 | **Never** set `Background`, `CornerRadius`, `Effect`, or `BorderBrush` directly on a section `Border`. |
| 2 | **Always** use the named card style — it provides surface color, stroke, radius, and shadow. |
| 3 | **One card per logical section** — do not nest cards inside cards. |

---

## 6  Form Sections Always Bottom-Aligned

| # | Rule |
|---|---|
| 1 | Collapsible add/edit forms go in `BottomFormContent` — the Auto-sized row **below** the star row. |
| 2 | When collapsed (`Visibility="Collapsed"`), the form row takes zero height. |
| 3 | When expanded, the form pushes up from the bottom; the star row absorbs the squeeze. |
| 4 | Error/success messages appear in the template's built-in message bar (Row 3) between the data and the form. |
| 5 | Forms must **never** be placed above the `DataGrid` in the visual stack — data is primary, forms are secondary. |
| 6 | Multiple mutually exclusive forms (add + edit) stack inside a single `StackPanel` in `BottomFormContent`. |

### Layout when form is visible

```
┌────────────────────────────────┐
│ TipBanner          (Auto)      │
├────────────────────────────────┤
│ Toolbar card       (Auto)      │
├────────────────────────────────┤
│                                │
│ DataGrid card      (*)         │  ← shrinks to accommodate form
│                                │
├────────────────────────────────┤
│ Error/Success      (Auto)      │  ← built-in, auto-collapses
├────────────────────────────────┤
│ Add/Edit form card (Auto)      │  ← visible, pushes up
├────────────────────────────────┤
│ StatusBar          (Auto)      │  ← optional
└────────────────────────────────┘
```

---

## 7  No Excessive Empty Space

| # | Rule |
|---|---|
| 1 | Unused `EnterprisePageLayout` slots auto-collapse via `Trigger` on `null` content. |
| 2 | Error/success messages auto-collapse when the bound text is empty or null. |
| 3 | Loading overlay is `Visibility="Collapsed"` by default — visible only when `IsLoading=True`. |
| 4 | **Never** add empty placeholder rows, spacer `Border` elements, or `Height` hacks for vertical alignment. |
| 5 | Spacing between rows comes from the card styles (`FormCardStyle.Margin`, `FieldGroupSpacing`) — not from empty rows. |
| 6 | `MinHeight="100"` on the main content `Grid` prevents the star row from collapsing to zero. |

---

## 8  No Full-Window ScrollViewer

| # | Rule |
|---|---|
| 1 | `ScrollViewer` must **never** wrap an entire `Window`, `Dialog`, or `UserControl` root. |
| 2 | `ScrollViewer` must **never** wrap a form-only subtree (no `DataGrid` / `ListView` / `ItemsControl` inside). |
| 3 | `ScrollViewer` is **only** allowed around data-driven content that can grow beyond its container. |
| 4 | `DataGrid` and `ListView` scroll internally — no external `ScrollViewer` needed. |
| 5 | `LayoutDiagnostics` (DEBUG-only) flags violations at runtime in the Output → Debug pane. |

### Allowed

| Context | Why |
|---|---|
| `DataGrid` / `ListView` / `ListBox` in a `*`-sized row | Dynamic row count; control scrolls internally |
| `ItemsControl` bound to a collection | Dynamic item count |
| Dynamic content pane (`ContentControl` in settings) | Content varies per tab |
| `TextBox` with `AcceptsReturn="True"` | Internal text overflow |
| `ResponsiveContentControl` (main content area) | Shell-level host with `ViewportConstrainedPanel` |

### Disallowed

| Context | Fix |
|---|---|
| `ScrollViewer` wrapping entire page | Use `EnterprisePageLayout` — star row handles sizing |
| `ScrollViewer` wrapping a form | Forms are bounded — use 4-column dense grid layout |
| `ScrollViewer` wrapping a dialog body | Use 3-row dialog grid (`Auto`/`*`/`Auto`) |

---

## 9  Compliance Checklist

Use this checklist when reviewing any page or dialog XAML:

### Page (EnterprisePageLayout)

- [ ] Root element is `controls:EnterprisePageLayout`
- [ ] No manual `Margin="{StaticResource PagePadding}"` on UserControl content
- [ ] No manual loading overlay or error/success message bar
- [ ] Tip banner in `TipBannerContent` slot with `TipKey` and `ContextKey`
- [ ] Toolbar in `ToolbarContent` slot using `SectionCardStyle`
- [ ] Primary data in `Content` using `SectionCardStyle` + `ClipToBounds="True"`
- [ ] DataGrid uses `EnterpriseDataGridStyle` with `MinHeight="100"`
- [ ] DataGrid has one `Width="*"` column
- [ ] Forms in `BottomFormContent` using `FormCardStyle`
- [ ] No `ScrollViewer` wrapping form-only content
- [ ] No multiple `*` rows in any `Grid` — exactly one star row per grid hierarchy
- [ ] No fixed pixel `Height` on any `RowDefinition`
- [ ] Form grids use only `Auto` + `FormRowSpacing` rows (no star row)
- [ ] No floating or absolutely positioned elements
- [ ] All spacing via design system tokens — zero inline values
- [ ] Every `Foreground`/`Background`/`BorderBrush` uses `{StaticResource Fluent…}`

### Dialog (BaseDialogWindow)

- [ ] Inherits `BaseDialogWindow`
- [ ] 3-row grid: `Auto` title / `*` body / `Auto` buttons
- [ ] `DialogWidth`/`DialogHeight` overrides in code-behind only
- [ ] No `Width`, `Height`, `ResizeMode` in XAML
- [ ] No full-body `ScrollViewer`

---

## 10  Enforcement Mechanisms

| Mechanism | Scope | When |
|---|---|---|
| `EnterprisePageLayout` template | Structural rows, star sizing, slot collapse | Compile-time (XAML) |
| `LayoutDiagnostics` behavior | ScrollViewer violations | DEBUG runtime |
| `StyleComplianceTests` | Inline colors, margins, fonts | CI / test run |
| Code review checklist (§9 above) | All rules | PR review |

---

## 11  Migration Path

Existing pages that use manual `Grid` + `RowDefinitions` at the root
should be migrated to `EnterprisePageLayout` during their next feature
touch.  Migration steps:

1. Replace root `<Grid Margin="{StaticResource PagePadding}">` with
   `<controls:EnterprisePageLayout>`.
2. Move tip banner into `TipBannerContent` slot.
3. Move toolbar into `ToolbarContent` slot.
4. Move primary data area as direct `Content`.
5. Move forms into `BottomFormContent` slot.
6. **Remove** manual error/success message bars — template provides them.
7. **Remove** manual loading overlays — template provides them.
8. **Remove** `Grid.Row` assignments — slots handle row placement.
9. Build and verify — zero XAML errors, zero `LayoutDiagnostics` warnings.
