# Printer Setup Runbook

## Purpose

Use this runbook when configuring or validating thermal printers and customer display hardware on a workstation.

## Thermal printer setup

1. Confirm the printer is visible in Windows printer settings.
2. Set the intended printer as the workstation default if the store policy requires it.
3. Open System Settings and confirm printer width and page size.
4. Print a preview first, then a live test receipt.
5. Verify paper width, margins, logo alignment, and barcode readability.

## Customer display checks

1. Confirm the secondary display or pole display is detected by Windows.
2. Validate the configured display target in hardware settings.
3. Run a sample billing flow and confirm totals update on the customer display.

## Failure handling

1. If print preview works but physical print fails, check Windows printer queue and driver status.
2. If the printer prints cropped output, confirm printer width and paper size in settings.
3. If customer display stays blank, check cable, Windows display mode, and workstation hardware service logs.

## Evidence to capture

- Workstation name
- Printer model
- Windows default printer
- Receipt preview screenshot
- Physical output issue description
