# Release Validation Checklist

## Before publish

1. Run `scripts\release-readiness.ps1`.
2. Run `scripts\performance-validation.ps1`.
3. Review the latest report under `artifacts\release-readiness`.
4. Review the latest support bundle export flow if there were recent failures.
5. Confirm there are no pending migrations that are unexpected for the release.

## Publish

1. Run `scripts\publish-release.ps1`.
2. Verify the publish output folder contains the application executable, appsettings, and assets.
3. Record the published version and output path.

## Post-publish smoke checks

1. Login and logout.
2. Dashboard loads without errors.
3. Billing flow creates a real sale.
4. Payment or debtor flow works.
5. Reports page loads and prints preview.
6. Backup page can list and verify backups.
7. Native notifications and in-app notifications still appear.

## Recovery readiness

1. Run `scripts\disaster-recovery-drill.ps1`.
2. Confirm the latest verified backup is recorded.
3. Confirm support bundle export works on the release build.
