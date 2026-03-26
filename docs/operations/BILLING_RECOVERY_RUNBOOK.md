# Billing Recovery Runbook

## Purpose

Use this runbook when billing is interrupted by application, printer, payment-reference, or database issues.

## Immediate triage

1. Ask the cashier to stop creating new bills until the current bill is understood.
2. Capture the visible invoice number or held bill context if available.
3. Export a support bundle before restarting the app.
4. Check whether the sale may already have been recorded by invoice number or idempotency symptoms.

## Duplicate or uncertain sale recovery

1. Search Sale History by time window, customer, or amount.
2. Confirm whether the invoice already exists.
3. If the invoice exists, do not re-bill the customer.
4. If payment was taken but the bill did not complete, reconcile against payment reference and cash drawer notes before retrying.

## Credit or split-payment recovery

1. Confirm whether debtor entry creation happened.
2. Confirm whether sale payment legs exist for the invoice.
3. If the sale exists but debtor amount is wrong, stop and escalate for supervised correction.
4. If the sale does not exist, re-enter the bill only after confirming stock was not decremented.

## Stock recovery checks

1. Re-open Product Management for the sold items.
2. Confirm current stock count versus expected stock count.
3. If stock changed without a final invoice, escalate before manual adjustment.

## Required post-incident checks

- Sale History entry
- Product stock
- Debtor balance if credit was involved
- Payment reference trail
- Printer output or reprint path
- Audit log entry if a corrective action was taken
