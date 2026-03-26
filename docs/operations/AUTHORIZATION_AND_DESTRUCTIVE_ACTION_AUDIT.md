# Authorization And Destructive Action Audit

## Scope

This audit focuses on actions that can delete, overwrite, restore, or materially alter financial and stock data.

## High-risk operations

- Database restore
- Backup cleanup
- Price override
- Discount approval by PIN
- Return processing
- Delete actions in master-data screens
- User and settings changes
- Inventory and inward corrections

## Required controls

1. Every destructive or financially material action must either require explicit confirmation, role validation, or both.
2. Every completed destructive or financially material action must create an audit log entry.
3. Recovery operations must be documented in the shift or release report.

## Current audit anchors

- `Core\Services\AuditService.cs`
- `Modules\Authentication\Services\LoginService.cs`
- `Modules\Billing\Services\BillingService.cs`
- `Modules\Backup\ViewModels\BackupRestoreViewModel.cs`

## Manual review points per release

1. Confirm destructive commands still surface confirmation UI.
2. Confirm restore, discount, return, and settings changes still log through the audit service.
3. Confirm user-management and admin-only surfaces remain behind the intended navigation and role flow.
4. Confirm no new raw delete paths were introduced without operator-visible recovery steps.
