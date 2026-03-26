# Backup And Restore Runbook

## Purpose

Use this runbook for routine backup checks, manual backups, backup validation, and controlled restore activity.

## Routine backup checks

1. Open the Backup page and confirm the backup folder path.
2. Verify that the latest backup timestamp is from the current business day.
3. Run backup verification against the latest `.bak`.
4. Confirm backup folder size growth is reasonable for current data volume.
5. Record the latest verified backup file name in the shift handover log.

## Manual backup before risky work

1. Confirm all operators have finished active billing.
2. Trigger `Backup now`.
3. Verify the new backup file.
4. Copy the verified backup to removable media before proceeding with upgrades or restore activity.

## Restore drill

1. Confirm the target backup file is the expected file by timestamp and file size.
2. Warn operators that restore is destructive and forces the database into single-user mode.
3. Export a support bundle before restore.
4. Capture the current application version and machine name.
5. Start the restore from the Backup page and wait for the success message.
6. Restart the application.
7. Validate login, dashboard load, recent sale history, products, and reports.
8. Record the restored backup file name, operator, and outcome in the drill log.

## Restore failure handling

1. Do not retry blindly if the first restore fails.
2. Export a support bundle immediately.
3. Verify the backup file with `RESTORE VERIFYONLY` through the application verify action.
4. Check whether another process still holds a SQL connection.
5. If restore still fails, move to the previous verified backup and record the incident.

## Evidence to capture

- Backup file name and timestamp
- Verification result
- Operator name
- Machine name
- Application version
- Post-restore smoke-check status
