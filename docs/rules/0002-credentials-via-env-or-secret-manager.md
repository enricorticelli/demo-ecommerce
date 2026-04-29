# R-002: Credentials via env vars or secret manager

- Status: enforced
- Date: 2026-04-29

## Rule

Load credentials from environment variables or a managed secret store. Do not hardcode.

## Why

Decouples deployment from source, enables rotation.

## Enforcement

Code review + manual audit.
