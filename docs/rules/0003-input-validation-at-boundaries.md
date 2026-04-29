# R-003: Input validation at boundaries

- Status: enforced
- Date: 2026-04-29

## Rule

Validate all external inputs (HTTP, queue, file, CLI) before use.

## Why

Prevents injection, unexpected state, and invariant violations.

## Enforcement

Code review + tests at boundaries.
