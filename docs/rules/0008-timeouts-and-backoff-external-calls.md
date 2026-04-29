# R-008: Timeouts and backoff for external calls

- Status: enforced
- Date: 2026-04-29

## Rule

Every outbound network call has a timeout. Retries use bounded backoff.

## Why

Prevents cascading hangs and thundering herds.

## Enforcement

Code review.
