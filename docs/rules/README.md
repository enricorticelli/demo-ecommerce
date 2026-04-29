# Rules Index

Non-negotiable constraints for demo-ecommerce.

Rules are imperative and short.
For conventions and style use `docs/guidelines/`.
For architectural decisions use `docs/adr/`.

## Status values

- enforced
- advisory

## Index

- [R-001 No secrets in repo](./0001-no-secrets-in-repo.md)
- [R-002 Credentials via env vars or secret manager](./0002-credentials-via-env-or-secret-manager.md)
- [R-003 Input validation at boundaries](./0003-input-validation-at-boundaries.md)
- [R-004 Least privilege for roles and keys](./0004-least-privilege-roles-keys.md)
- [R-005 Formatter and linter must pass in CI](./0005-formatter-linter-must-pass-ci.md)
- [R-006 Every bug fix adds a regression test](./0006-bug-fix-adds-regression-test.md)
- [R-007 No credentials or PII in logs](./0007-no-credentials-or-pii-in-logs.md)
- [R-008 Timeouts and backoff for external calls](./0008-timeouts-and-backoff-external-calls.md)
- [R-009 Backend services must not publish host ports](./0009-backend-services-no-host-ports.md)
