# Codebase Task Proposals

## 1) Typo task
**Task:** Fix the wording typo in the test section comment `State freetext validation on Save` to `State free-text validation on Save`.

**Why:** Even though this is only a comment, typo-free test names/comments improve readability and make search/indexing cleaner for maintenance work.

**Evidence:** The comment appears directly above `Save_InvalidStateText_ShowsError`.

## 2) Bug-fix task
**Task:** Tighten the GSTIN format validation regex so the 14th character is enforced as `Z` (current implementation allows any alphanumeric there).

**Why:** The current regex permits values like `22AAAAA0000A1A5`, which are accepted by format validation despite violating the canonical GSTIN structure.

**Evidence:** The generated regex is `^\d{2}[A-Z]{5}\d{4}[A-Z]\d[A-Z][A-Z\d]$` and tests explicitly confirm non-`Z` acceptance at position 14.

## 3) Documentation/comment discrepancy task
**Task:** Reconcile `AppBranding` XML docs with actual implementation (or update implementation), because the docs describe `logo-64.png` checks while code checks `logo-256.png`.

**Why:** This mismatch can mislead developers about which asset controls fallback behavior and may cause incorrect debugging assumptions when branding assets are missing.

**Evidence:** Class/property comments mention `logo-64.png`, but `HasLogoAsset` loads `pack://application:,,,/Assets/logo-256.png`.

## 4) Test improvement task
**Task:** Strengthen `Save_InvalidStateText_ShowsError` so it also proves that save side effects do **not** occur (e.g., no success message, no close request, and no persistence call if mockable), not just that an error string is set.

**Why:** A regression could set an error message and still execute save/persist logic; the current test would not catch that.

**Evidence:** The current test asserts only `Assert.Contains("valid Indian state", sut.ErrorMessage);` after executing `SaveCommand`.
