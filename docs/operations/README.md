# StoreAssistantPro Operations

This folder holds the operator-facing runbooks and release checklists for StoreAssistantPro.

Recommended execution order:

1. Read `RELEASE_VALIDATION_CHECKLIST.md`.
2. Run `scripts\release-readiness.ps1`.
3. Run `scripts\performance-validation.ps1` for a seeded large-data pass.
4. Use `scripts\publish-release.ps1` for release packaging.
5. Use `scripts\export-support-bundle.ps1` when investigating field issues.
6. Use `scripts\disaster-recovery-drill.ps1` before a release cut or recovery rehearsal.

Runbooks in this folder:

- `BACKUP_AND_RESTORE_RUNBOOK.md`
- `BILLING_RECOVERY_RUNBOOK.md`
- `PRINTER_SETUP_RUNBOOK.md`
- `RELEASE_VALIDATION_CHECKLIST.md`
- `AUTHORIZATION_AND_DESTRUCTIVE_ACTION_AUDIT.md`
