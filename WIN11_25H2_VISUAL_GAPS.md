# Windows 11 25H2 Visual Gaps — Implementation Phases

> **154 enhancements** organized into **10 phases** by dependency order, risk, and impact.
>
> **Principle**: Token → Style → Template → Behavior → Feature.
> Earlier phases unlock later ones. Each phase is independently shippable.

---

## Current State Summary

| File | Role | Status |
|---|---|---|
| `DesignSystem.xaml` | Primitive tokens (colors, spacing, radii, durations, shadows) | ✅ Comprehensive |
| `FluentTheme.xaml` | Control templates (TextBox, Button, ComboBox, Menu, etc.) | ✅ Good base |
| `MotionSystem.xaml` | Reusable storyboards + `h:Motion` attached behaviors | ✅ Fade/scale only |
| `GlobalStyles.xaml` | Implicit styles, validation, app-level overrides | ✅ Solid |
| `PosStyles.xaml` | POS-specific templates (PIN, role selection, billing) | ✅ Minimal |
| `ToggleSwitch.xaml` | Custom toggle switch control | ✅ Already pill-shaped |

---

## Phase 1 — Foundation Tokens & Color System
**Scope**: `DesignSystem.xaml` only — zero template changes.
**Risk**: 🟢 Very Low — additive token definitions, existing consumers unaffected.

| # | Enhancement | What to Do |
|---|---|---|
| 1 | Semi-transparent surface tint | Add `LayerFillColorDefault` (`#80F9F9F9`). Update `FluentSurface` to use it for cards. |
| 3 | Subtle divider stroke | Add `DividerStrokeColorDefault` (`#0D000000`). |
| 7 | Consistent 4px corner radius | Audit all `FluentCorner*` tokens — unify to `ControlCornerRadius=4`. |
| 13 | System accent color | Add `SystemAccentColor` resource with fallback `#005FB8`. Optionally read from registry at startup. |
| 29 | Spacing system 4px grid | Audit all spacing tokens — ensure all are multiples of 4 (4, 8, 12, 16, 24, 32). |
| 58 | Layered z-depth system | Define 3 shadow tokens: `ElevationBase` (none), `ElevationCard` (subtle), `ElevationFlyout` (medium). |
| 75 | High-contrast aware tokens | Add `SystemColors` fallback mappings for all primary tokens. |
| 111 | Text selection accent color | Add `SelectionHighlightColor` at `AccentDefault` 40% opacity. |
| 125 | Disabled state opacity | Add `DisabledOpacity` token (`0.4`). |
| 131 | System backdrop fallback | Add `SolidBackgroundFillColorBase` with `CardStrokeColorDefault` inner border. |
| 132 | Menu separator styling | Add `MenuSeparatorStroke` token (`#0D000000`, 1px, 12px horizontal padding). |

**Deliverable**: ~15 new tokens in `DesignSystem.xaml`. No visual regressions.

---

## Phase 2 — Surface Depth & Card System
**Scope**: `DesignSystem.xaml` shadows + `FluentTheme.xaml` card styles + `GlobalStyles.xaml`.
**Risk**: 🟢 Low — style changes, no template rewrites.
**Depends on**: Phase 1 (tokens).

| # | Enhancement | What to Do |
|---|---|---|
| 2 | Elevation shadow on cards | Replace hard `BorderThickness="1"` cards with soft `0 2 4 rgba(0,0,0,0.04)` ambient `DropShadowEffect`. |
| 40 | Hover reveal border on cards | Card border invisible at rest, appears `SubtleFillColorTertiary` on hover. |
| 99 | Card hover lift | TranslateY −2px + shadow deepens on hover (150ms ease-out). |
| 154 | Active/inactive window dimming | Dim title bar text to `FluentTextTertiary` + reduce border accent when window inactive. |

**Deliverable**: Cards gain depth, hover interaction, and ambient shadow.

---

## Phase 3 — Core Control Restyling
**Scope**: `FluentTheme.xaml` templates, `GlobalStyles.xaml` implicit styles.
**Risk**: 🟡 Medium — template changes affect all instances globally.
**Depends on**: Phase 1, Phase 2.

### 3A — Input Controls

| # | Enhancement | What to Do |
|---|---|---|
| 22 | Input field idle border | Rest: transparent top/side, bottom 1px stroke only. Full border on hover/focus. |
| 42 | Search box dismiss ✕ | Add clear button inside TextBox template, visible when `Text.Length > 0`. |
| 90 | Input prefix/suffix adornments | Add `Prefix`/`Suffix` attached properties for inline labels (₹, kg). |
| 110 | Password reveal eye | Add 👁 toggle button in `FluentPasswordBoxStyle` template. |
| 113 | Field auto-grow TextBox | Multi-line TextBox auto-grows to max with 100ms height animation. |
| 152 | Field placeholder animation | Placeholder slides up 4px + scales 75% → becomes floating label on focus. |

### 3B — Buttons

| # | Enhancement | What to Do |
|---|---|---|
| 30 | Button icon + text pattern | Add `IconButtonStyle` with 16px Segoe Fluent Icons glyph + 8px gap + label. |
| 66 | Button pressed scale | Scale to 0.98 on pressed for 60ms, spring back. |
| 70 | Gradient accent on primary | Primary button: subtle 2-stop vertical gradient (AccentLight1 → AccentDefault). |
| 100 | Split button pattern | Create `SplitButton` control template — primary click + dropdown chevron. |
| 101 | Async command button state | Spinner replaces icon during async operations, button disabled until complete. |

### 3C — Selection Controls

| # | Enhancement | What to Do |
|---|---|---|
| 21 | Checkbox/RadioButton restyle | Rounded 4px checkbox (filled accent + white checkmark), pill-shaped radio. |
| 45 | Toggle switch modernization | Already pill-shaped in `ToggleSwitch.xaml` — refine to match Win11 exactly. |

### 3D — Dropdowns & Popups

| # | Enhancement | What to Do |
|---|---|---|
| 15 | Tooltip modernization | Rounded corners, soft shadow, 12px padding, no border. |
| 17 | Context menu styling | Rounded corners, padding, acrylic background, soft shadow. |
| 18 | ComboBox dropdown | 8px corner radius popup, subtle shadow, slide-down entrance animation. |
| 37 | Flyout menus | Attached flyout panels with rounded corners, 4px inner padding, slide-in. |
| 46 | Calendar/DatePicker flyout | Rounded flyout calendar with month/year navigation, today highlight ring. |
| 139 | Auto-complete dropdown | Auto-suggest box with filtered results flyout and keyboard navigation. |

### 3E — Other Controls

| # | Enhancement | What to Do |
|---|---|---|
| 8 | Typography weight refinement | Body text → Regular (400). SemiBold (600) only for titles/labels. |
| 20 | ProgressBar styling | Thin 2px accent indeterminate bar with parabolic sweep. |
| 41 | NumberBox stepper | Create `NumberBox` with +/− stepper buttons, spin on scroll, validation. |
| 109 | Cursor style | Hand cursor on all clickable non-button elements (links, cards, tags, pills). |
| 114 | Hover underline on links | Underline on hover only, AccentDefault color, no underline at rest. |

**Deliverable**: All core controls match Win11 25H2 look and feel.

---

## Phase 4 — Scrollbar, Menu & Chrome
**Scope**: `FluentTheme.xaml` chrome templates.
**Risk**: 🟡 Medium — global visual changes.
**Depends on**: Phase 1, Phase 3.

| # | Enhancement | What to Do |
|---|---|---|
| 14 | Thin overlay scrollbars | 2px resting → 6px on hover, overlay style, subtle thumb. |
| 23 | Menu bar hover pills | Pill-shaped hover highlight (not full-width rectangle), `SubtleFillColorSecondary`. |
| 12 | Status bar / command bar | Top-aligned command bar look with `ChromeAltFillColorSecondary`. |
| 57 | Keyboard shortcut hints in menus | Right-aligned shortcut text in `FluentTextTertiary` color. |
| 122 | Grouped command spacing in menus | 8px separator gaps between logical groups, 0px within groups. |

**Deliverable**: Menu, scrollbar, and chrome match Win11 aesthetic.

---

## Phase 5 — DataGrid & Table System
**Scope**: `FluentTheme.xaml` + `GlobalStyles.xaml` DataGrid styles.
**Risk**: 🟡 Medium — DataGrid is complex, many modules depend on it.
**Depends on**: Phase 1, Phase 3.

### 5A — Row & Cell Styling

| # | Enhancement | What to Do |
|---|---|---|
| 4 | Rest/Hover/Pressed state layering | 3-state subtle fill transitions on DataGrid rows, ListBox items. |
| 9 | Compact density 32px rows | Add `CompactDataGridRowHeight` token (32px) with tighter padding. |
| 24 | Alternating row tint | Alternate between `#FFFFFF` and `#F9F9F9`. |
| 60 | Selection highlight rounded | 4px rounded pill highlight with `SubtleFillColorSecondary`. |
| 118 | Right-click row selection | Auto-select right-clicked row before showing context menu. |
| 121 | Keyboard nav ring on cell | Rounded ring on individual cell, not whole row. |
| 140 | Empty DataGrid placeholder | Centered single row: "No items to display" in `FluentTextSecondary`. |

### 5B — Column Headers

| # | Enhancement | What to Do |
|---|---|---|
| 38 | Column header styling | Uppercase 11px Caption weight, no bottom border, whitespace separation. |
| 76 | Column resize handle | Thin accent-colored 2px drag handle on header hover. |
| 89 | Persistent sort indicator | Subtle accent ▲/▼ chevron on sorted column at all times. |
| 135 | Proportional column auto-sizing | Auto-distribute remaining width proportionally across `*` columns. |

### 5C — Advanced Table Features

| # | Enhancement | What to Do |
|---|---|---|
| 61 | Multi-select checkbox column | Circular checkbox on hover at row left edge, filled accent on selection. |
| 62 | Inline command buttons on hover | Action icons (Edit ✏️, Delete 🗑️) right-aligned on hover, 100ms fade-in. |
| 85 | Grouped DataGrid rows | Collapsible group headers with item count badge and alternating group tint. |
| 86 | Drag-to-reorder columns | Ghost header preview + insertion caret indicator. |
| 103 | Pinned/sticky table header | Column headers pinned at scroll viewport top with bottom shadow. |
| 119 | Column visibility toggle | ⚙ column chooser button in header area with checkboxes. |

**Deliverable**: DataGrid is fully Win11-styled with compact mode support.

---

## Phase 6 — Motion & Animation System
**Scope**: `MotionSystem.xaml` + `Core/Helpers/Motion.cs` + page containers.
**Risk**: 🟡 Medium — animations can cause janky UX if timings are wrong.
**Depends on**: Phase 1.

### 6A — Page Transitions

| # | Enhancement | What to Do |
|---|---|---|
| 11 | Page slide + fade entrance | 16px translateY + opacity over 250ms with decelerate easing. |
| 106 | Animated page exit | Opacity 1→0, translateY 0→−8px, 100ms before new page enters. |
| 120 | Crossfade view switch | Old view fades 100ms, new fades in 150ms with 50ms overlap. |
| 150 | Content transition clip | Clip outgoing page to bounds during exit animation. |

### 6B — Item Animations

| # | Enhancement | What to Do |
|---|---|---|
| 27 | Animated expand/collapse | 150ms height animation with decelerate easing on Expander/panels. |
| 34 | Entrance stagger | List/card items animate in with 30ms stagger per item. |
| 55 | Section collapse with chevron | Animated chevron (▸→▾ rotation) + content slide-down. |
| 72 | Tab/close animation | Shrink horizontally + fade out 150ms, siblings slide to fill. |
| 137 | Smooth accordion | HeightAnimation (0→Auto) with 200ms decelerate + chevron 90° rotation. |
| 153 | Staggered card entrance | Dashboard cards animate in with 50ms stagger per card. |

### 6C — Data Animations

| # | Enhancement | What to Do |
|---|---|---|
| 59 | Smooth scroll with inertia | Pixel-based kinetic scrolling with deceleration curve. |
| 77 | Animated count transitions | KPI numbers animate old → new value with counting interpolation. |
| 81 | Smooth column sort animation | Rows crossfade 100ms on sort change. |
| 97 | Animated number badge | Scale bounce 1.0→1.2→1.0 over 200ms when count changes. |

### 6D — Micro-Interactions

| # | Enhancement | What to Do |
|---|---|---|
| 64 | Connected animation between views | Clicked card morphs/flies into detail view. |
| 83 | Pressed ripple effect | Radial opacity ripple from click point outward over 200ms. |
| 126 | Micro-vibration on error | 4px horizontal shake (3 oscillations, 300ms) on validation failure. |
| 145 | Dark ink well on button press | Dark-to-light radial ink spread from press point over 150ms. |

**Deliverable**: Full motion system matching Win11 fluidity.

---

## Phase 7 — Dialog & Window Chrome
**Scope**: `BaseDialogWindow`, `WindowSizingService`, dialog templates.
**Risk**: 🟡 Medium — dialog infrastructure is frozen (extend only).
**Depends on**: Phase 1, Phase 2.

| # | Enhancement | What to Do |
|---|---|---|
| 19 | Dialog window chrome | No visible title bar text, in-content header, Smoke overlay on owner. |
| 65 | Dialog smoke overlay | Dim owner to 40% opacity with `SystemFillColorSolidNeutralBackground`. |
| 107 | Frosted glass dialog header | 8px blur, 60% white overlay strip separating title from content. |
| 108 | Resize grip removed | Zero resize affordance on NoResize dialogs. |
| 115 | Login background | Soft radial gradient blob (accent at 5% opacity) behind centered card. |
| 117 | Sticky footer action bar | Pin Save/Cancel with top shadow separator — buttons never scroll. |
| 91 | Stepped wizard progress bar | Segmented horizontal bar — completed fills accent, current pulses. |

**Deliverable**: Dialogs feel native Win11 with proper overlay and depth.

---

## Phase 8 — Component Library & Patterns
**Scope**: New custom controls in `Core/Controls/`, new styles in `GlobalStyles.xaml`.
**Risk**: 🟡 Medium — new controls need testing across all modules.
**Depends on**: Phase 1–3.

### 8A — Status & Feedback

| # | Enhancement | What to Do |
|---|---|---|
| 6 | InfoBar inline banners | Create `InfoBar` control — colored strips (Success/Warning/Error/Info) inline. |
| 36 | Color-coded status pills | Rounded pills: Paid/Pending/Overdue with tinted background + matching text. |
| 73 | Color-coded tag chips | Small rounded chips with tinted background matching semantic color. |
| 80 | Toast stacking | Stack up to 3 toasts with 4px gap, each with own dismiss timer + slide-out. |
| 116 | Toast icon per severity | Prefix: ✓ success/green, ⚠ warning/amber, ✕ error/red, ℹ info/accent. |
| 71 | Clipboard toast on copy | Brief "Copied to clipboard" toast with checkmark, auto-dismiss 2s. |
| 105 | Status dot on connection | 8px circle — green connected, red disconnected, amber degraded. |

### 8B — Navigation & Wayfinding

| # | Enhancement | What to Do |
|---|---|---|
| 5 | Pill-shaped selection indicator | Small rounded accent pill on selected NavigationView item's left edge. |
| 26 | Breadcrumb path | Settings-style > chevron breadcrumb trail in title area. |
| 28 | Badge indicators on menu items | Small accent dot/count badges on NavigationView items. |
| 31 | Segmented control (tab pills) | Pill-shaped segmented toggles for view switching (List/Grid/Details). |
| 88 | Command palette / quick switcher | Ctrl+K command palette — fuzzy search across pages, actions, recent. |
| 63 | Sidebar navigation rail | Left 48px icon rail (expandable to 320px) with animated hamburger. |
| 112 | Animated hamburger ↔ back | Morph ☰ into ← with 200ms rotation + path interpolation. |

### 8C — Data Display

| # | Enhancement | What to Do |
|---|---|---|
| 25 | Empty state patterns | Centered icon + title + subtitle + optional CTA for empty collections. |
| 32 | Compact number formatting | ₹1.2L / ₹45K style compact numbers in KPI cards. |
| 78 | Error page pattern | Centered sad-face illustration + title + description + retry button. |
| 82 | Date relative formatting | "Just now", "5 min ago", "Yesterday" for recent, full date for older. |
| 127 | KPI trend arrow indicators | ↑ green / ↓ red / → gray trend arrows next to KPI values. |
| 128 | Overflow ellipsis with tooltip | Any `TextTrimming="CharacterEllipsis"` auto-shows full text tooltip. |
| 133 | Multi-line truncation | Clamp to 2-3 lines with "Show more" link + smooth height animation. |

### 8D — Form Patterns

| # | Enhancement | What to Do |
|---|---|---|
| 44 | Grouped settings layout | Card-grouped sections with bold group header, full-width rows. |
| 54 | Inline validation icon | ⚠ or ✕ icon inside field's right edge alongside error border. |
| 96 | Field-level help tooltip (ℹ) | Small ⓘ circle next to complex fields with rich tooltip explanation. |
| 124 | Inline date range picker | Single date range control with two side-by-side calendars. |
| 102 | Filter chips bar | Horizontally scrollable rounded chips for active filters with ✕ dismiss. |

**Deliverable**: Full component library for Win11-native UI patterns.

---

## Phase 9 — Accessibility, Polish & Platform Integration
**Scope**: Cross-cutting concerns, system integration.
**Risk**: 🟡 Medium — some require platform interop.
**Depends on**: Phase 1–8.

### 9A — Focus & Keyboard

| # | Enhancement | What to Do |
|---|---|---|
| 16 | Focus visible indicator | 2px double-stroke ring (inner white, outer black). |
| 48 | Reveal focus on keyboard nav | Soft gradient glow following focused item. |
| 146 | Accessible name on all icons | Set `AutomationProperties.Name` on every icon-only button. |

### 9B — Platform Integration

| # | Enhancement | What to Do |
|---|---|---|
| 84 | Auto-theme sync with Windows | Read `AppsUseLightTheme` registry key, sync accent color. |
| 142 | Window title matches state | Dynamic taskbar title: "Billing #1042 — StoreAssistantPro". |
| 148 | System notification integration | Push to Windows Notification Center via `AppNotificationManager`. |
| 74 | Soft keyboard integration | Auto-scroll focused fields above touch keyboard. |
| 141 | Numeric keypad inputs | Input scope hints for phone, pincode, quantity, price on touch. |

### 9C — Visual Polish

| # | Enhancement | What to Do |
|---|---|---|
| 10 | Fluent system icons | Audit all icon usage — migrate MDL2 → Segoe Fluent Icons. |
| 49 | Background noise texture | 1-2% opacity noise grain overlay for organic depth. |
| 79 | Responsive font scaling | PageTitle 28→24 at <1440px, hide subtitle text. |
| 129 | Accent-tinted icon fills | Accent fills on selected/active icons, `FluentTextSecondary` unselected. |
| 130 | Print-friendly mode | Strip shadows, backgrounds, accent colors for print. |
| 134 | RTL mirror awareness | Logical Start/End padding, flip chevrons and icons. |

### 9D — Behavioral Polish

| # | Enhancement | What to Do |
|---|---|---|
| 47 | Time-based greeting | "Good morning, Name" contextual greeting in workspace header. |
| 52 | Avatar/initials circle | Colored circle with user initials in status bar and profile. |
| 53 | Notification bell pulse | Red dot on bell for unread, pulse once on new arrival. |
| 68 | Compact mode toggle | Compact/Normal density switch — Compact reduces row height globally. |
| 87 | Snap layout hover zones | Snap layout options on maximize button hover for multi-dialog. |
| 147 | Zoom level persistence | Remember user's last zoom level per page, restore on re-entry. |
| 149 | Pointer hover delay for tooltips | 500ms for info, 0ms for error/validation — differentiated by severity. |

**Deliverable**: Accessibility-complete, system-integrated, polished UI.

---

## Phase 10 — Advanced Interactions & Future Features
**Scope**: Complex interactive patterns requiring significant C# + XAML work.
**Risk**: 🔴 Higher — new behaviors, drag/drop, virtualization.
**Depends on**: Phase 1–8.

### 10A — Inline Editing & Drag

| # | Enhancement | What to Do |
|---|---|---|
| 39 | Inline editable fields | Click-to-edit on table cells with pencil icon on hover. |
| 51 | Drag handle affordance | 6-dot grip handle on hover at row left edge. |
| 93 | Inline rename | Explorer-style inline rename, cell text becomes editable. |
| 136 | Drag selection rectangle | Rubber-band drag to select multiple items with accent rectangle. |
| 143 | Row drag reorder | 2px accent horizontal line between rows for drop indicator. |

### 10B — Loading & Skeleton

| # | Enhancement | What to Do |
|---|---|---|
| 33 | Skeleton loading placeholders | Shimmering gray rectangles matching content shapes during load. |
| 94 | Loading shimmer on DataGrid | Pulsing gray placeholder rows matching column widths during load. |

### 10C — Advanced UI Patterns

| # | Enhancement | What to Do |
|---|---|---|
| 35 | TeachingTip callouts | Pointed callout bubbles anchored to specific controls. |
| 43 | Command bar overflow | Collapse overflow items into ··· more button flyout at narrow widths. |
| 50 | Responsive column breakpoints | Collapse side panels below certain widths, stack vertically. |
| 56 | Print preview modernization | In-app preview pane with zoom slider and page navigation. |
| 67 | Auto-hiding header on scroll | Auto-hide title/command bar on scroll down, reveal on scroll up. |
| 69 | TreeView indent lines | Thin dotted vertical indent guides connecting parent-child nodes. |
| 92 | Swipe-to-dismiss toasts | Horizontal swipe gesture and drag-away for touch. |
| 95 | Color-coded sidebar indicators | 3px left accent bar on selected, colored dot on notification. |
| 98 | Header parallax on scroll | Hero/banner scrolls at 50% speed relative to content. |
| 104 | Icon-only compact toolbar | Collapse to icon-only at narrow widths, tooltip shows label on hover. |
| 123 | Header action alignment | Page headers always right-align actions on same baseline as title. |
| 138 | Command bar elevation on scroll | Bottom 1px shadow appears on first scroll pixel. |
| 144 | Floating action button (FAB) | Bottom-right circular accent FAB for primary action (e.g., New Sale). |
| 151 | Semantic zoom on large lists | Pinch-to-zoom-out grouped jump list (A-Z, categories). |

**Deliverable**: Full Win11 25H2 parity for advanced interaction patterns.

---

## Cross-Phase Items (Apply Everywhere)

These apply across all phases as their respective controls are touched:

| # | Enhancement | Applies When |
|---|---|---|
| 7 | Consistent 4px corner radius | Every control template touched |
| 8 | Typography weight refinement | Every text element touched |
| 29 | Spacing system 4px grid | Every margin/padding touched |
| 109 | Cursor style on interactive | Every clickable element added |
| 125 | Disabled state 0.4 opacity | Every control template touched |
| 146 | Accessible name on icons | Every icon-only button added |

---

## Phase Dependency Graph

```
Phase 1 (Tokens)
  ├── Phase 2 (Cards & Depth)
  │     └── Phase 7 (Dialog Chrome)
  ├── Phase 3 (Core Controls)
  │     ├── Phase 4 (Scrollbar & Menu)
  │     ├── Phase 5 (DataGrid)
  │     └── Phase 8 (Component Library)
  ├── Phase 6 (Motion System)
  └── Phase 9 (Accessibility & Polish)
        └── Phase 10 (Advanced Interactions)
```

---

## Implementation Rules

1. **All new tokens** go in `DesignSystem.xaml` — zero magic numbers in templates.
2. **All new control templates** go in `FluentTheme.xaml`.
3. **All new implicit/named styles** go in `GlobalStyles.xaml`.
4. **All new animations** go in `MotionSystem.xaml` or `h:Motion.*` attached behaviors.
5. **All new custom controls** go in `Core/Controls/`.
6. **Use `StaticResource`** not `DynamicResource` (per project rules).
7. **Test at 1920×1080** — the target resolution.
8. **Build verification** after each phase before proceeding.

---

## Summary

| Phase | Items | Scope | Risk | Dependencies |
|---|---|---|---|---|
| **1** | 11 | Foundation tokens | 🟢 Very Low | None |
| **2** | 4 | Surface & depth | 🟢 Low | Phase 1 |
| **3** | 22 | Core controls | 🟡 Medium | Phase 1–2 |
| **4** | 5 | Scrollbar & menu chrome | 🟡 Medium | Phase 1, 3 |
| **5** | 18 | DataGrid & tables | 🟡 Medium | Phase 1, 3 |
| **6** | 18 | Motion & animation | 🟡 Medium | Phase 1 |
| **7** | 7 | Dialog & window chrome | 🟡 Medium | Phase 1–2 |
| **8** | 26 | Component library | 🟡 Medium | Phase 1–3 |
| **9** | 20 | Accessibility & polish | 🟡 Medium | Phase 1–8 |
| **10** | 23 | Advanced interactions | 🔴 Higher | Phase 1–8 |
| **Total** | **154** | | | |
