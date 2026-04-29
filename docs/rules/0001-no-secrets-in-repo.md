# R-001: No secrets in repo

- Status: enforced
- Date: 2026-04-29

## Rule

Never commit credentials, tokens, keys, or private material.

## Why

Leaked secrets are costly and often irrecoverable.

## Enforcement

Pre-commit secret scanner + CI gate.
