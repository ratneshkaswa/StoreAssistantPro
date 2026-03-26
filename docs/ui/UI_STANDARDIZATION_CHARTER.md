# UI Standardization Charter

This document is the product-level source of truth for UI standardization in StoreAssistantPro.

## Source Of Truth

The primary implementation layer lives in:

- `Core/Styles/DesignSystem.xaml`
- `Core/Styles/GlobalStyles.xaml`
- `Core/Styles/FluentTheme.xaml`
- `Core/Styles/PosStyles.xaml`
- `StoreAssistantPro.Tests/Helpers/UiStandardizationStandardsTests.cs`

Views should consume shared styles and tokens from those files instead of introducing page-owned layout, action, or semantic variants.

## Universal Action Rules

- `Save`, `Create`, `Confirm`, and `Run` are the primary commit actions.
- `Cancel` aborts an edit flow.
- `Close` dismisses a panel, overlay, preview, or dialog.
- `Back` navigates to the previous surface.
- `Delete`, `Reset`, `Restore`, and other destructive actions must use destructive styling and shared confirmation flows.
- `Refresh`, `Search`, `View`, `Import`, `Export`, and `Print` use semantic utility styles instead of generic primary or secondary buttons.
- Explicit `Search` buttons are reserved for apply-search flows with multiple criteria or non-live queries. Debounced single-field search rows should not add a redundant `Search` button.
- Text-like commands should use shared link-button styles, not raw `Hyperlink` markup or ad hoc text actions.

## Page Classes

The app uses a small set of page-shell classes:

- `Centered surface pages` for authentication and focused setup forms.
- `Master-detail pages` for list-plus-editor or list-plus-detail workflows.
- `Compact tool pages` for production tools such as barcode labels.
- `Operational pages` for CRUD and transaction entry.
- `Analytical report pages` for reporting and export-heavy views.

Representative styles and tokens:

- `CenteredSurfaceCardStyle`
- `CenteredPageContentHostStyle`
- `MasterDetailPageGridStyle`
- `SplitToolPageContentGridStyle`
- report summary and section-card styles

## State Surfaces

- Loading overlays should bind through the shared working-state contract on `BaseViewModel`.
- Representative views that need one overlay for both loading and busy states should bind `LoadingOverlay` to `IsWorking` and `WorkingMessage`.
- Status messages should use `InfoBar` with `IsOpen` state bindings.
- Validation summaries should use the shared validation-summary card styles on long-form editors.
- Empty states should use shared glyph resources and the app-wide voice model: title begins with `No ` and descriptions read as complete sentences.
- Read-only previews and detail overlays should use dedicated shared surface styles instead of reusing edit-surface chrome.

## Input Rules

- Required fields use `RequiredFieldIndicatorRunStyle`.
- Money fields use `CurrencySymbol` adornments, `h:NumericInput.Scope="Number"`, and `h:NumericInput.IsDecimalOnly="True"`.
- Telephone and PIN fields use telephone/digit numeric scopes instead of free-form text input when appropriate.
- Field widths should come from shared width classes or page tokens rather than raw literals.
- Placeholder copy should be descriptive, entity-specific, and avoid shorthand such as `Qty`, `Ref #`, or `e.g.`-driven watermarks in module XAML.

## Dialog Taxonomy

The app distinguishes three dialog types:

- `Confirmation dialogs` for confirm/cancel decisions, including destructive flows.
- `Edit dialogs` for focused data entry when inline or side-panel editing is not appropriate.
- `Preview or inspection dialogs` for read-only content such as print or receipt preview.

These dialog types must not collapse into one generic chrome. Footer order, default actions, and close semantics should reflect the dialog type.

## Edit Surface Decision Rules

- Use inline row editing for small, reversible edits inside a dense list.
- Use a side editor or paired detail pane for entity editing when list context must remain visible.
- Use a dedicated dialog only when the task is focused, short-lived, and should block the underlying surface.
- Use a sticky footer for long-form commit surfaces.
- Use localized form action rows for shorter edit surfaces.

## Disclosure And Overflow Rules

- Primary actions remain directly visible.
- Secondary but common actions may live in a section command bar.
- Advanced or state-transition actions may use split buttons or shared overflow surfaces.
- Dense action rows and segmented filters must wrap responsively rather than forcing horizontal scrolling.
- Row-level object actions should use consistent context-menu and hover-reveal patterns.

## Reporting Rules

- Report pages use analytical summary strips, report section cards, analytical table styling, and semantic export actions.
- Export actions are non-destructive utility commands and should remain visually separate from mutating actions.

## Responsive And Hover Rules

- Horizontal scrolling is not acceptable for standard forms and filter bars.
- Hover-reveal affordances should use shared styles instead of per-template animation logic.
- Overlay widths, command-palette widths, and similar shell/transient dimensions must be tokenized.

## Governance

- New pages should start from shared page-shell classes and semantic action styles.
- New exceptions must be justified in code review and should usually result in a shared style or token, not another local one-off.
- `UiStandardizationStandardsTests` is the automated guardrail for this charter.
