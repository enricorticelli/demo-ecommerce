# R-007: No credentials or PII in logs

- Status: enforced
- Date: 2026-04-29

## Rule

Structured logs must not include secrets, tokens, or unnecessary personal data.

## Why

Logs aggregate into long-lived stores with broad access.

## Enforcement

Code review + log scrubbers where available.
