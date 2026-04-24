# Runbook: Seed Catalog Data

The seeding script (`scripts/seeding/seed-catalog.js`) populates the Catalog service with brands, categories, collections, and products via the Gateway API.

## Prerequisites

- Full stack running (`docker compose up`)
- Gateway API healthy at `PUBLIC_GATEWAY_URL` (default: see `.env`)
- Node.js ≥ 20.3.0 installed

## Default seed volumes

| Entity      | Count |
|-------------|-------|
| Brands      | 40    |
| Categories  | 80    |
| Collections | 50    |
| Products    | 2000  |

## Run the seed

```bash
node scripts/seeding/seed-catalog.js
```

## Dry run (no writes)

```bash
node scripts/seeding/seed-catalog.js --dry-run
```

## Verbose output

```bash
node scripts/seeding/seed-catalog.js --verbose
```

## Auth modes

By default the script runs with `SEED_AUTH_MODE=auto`:

- it starts without `Authorization`
- if the Gateway answers `401` or `403`, it retries with a bearer token
- token acquisition uses, in order: `SEED_BEARER_TOKEN` or Keycloak client credentials

To force JWT usage from the beginning:

```bash
node scripts/seeding/seed-catalog.js --auth-mode required
```

## Environment overrides

| Variable           | Default                          | Purpose                          |
|--------------------|----------------------------------|----------------------------------|
| `PUBLIC_GATEWAY_URL` | Read from `.env`               | Base URL for Gateway API         |
| `SEED_CONCURRENCY` | 5                                | Parallel request pool size       |
| `SEED_TIMEOUT_MS`  | 10000                            | Per-request timeout (ms)         |
| `SEED_AUTH_MODE`   | `auto`                           | `auto`, `required`, or `off`     |
| `SEED_BEARER_TOKEN` | unset                           | Explicit bearer token override   |
| `SEED_KEYCLOAK_AUTHORITY` | `BACKOFFICE_KEYCLOAK_AUTHORITY` | Realm authority for token acquisition |
| `SEED_CLIENT_ID`   | `Gateway__Auth__ClientId`        | Client ID for `client_credentials` |
| `SEED_CLIENT_SECRET` | `Gateway__Auth__ClientSecret`  | Client secret for `client_credentials` |

## Notes

- The script is idempotent: it deletes existing data before seeding.
- Existing entities are fetched page by page before reset, so reruns delete the full catalog instead of just the first page.
- Retry logic uses exponential backoff (see `scripts/seeding/seed-catalog.js` lines 234–291).
- Correlation IDs are logged per batch for debugging.
- If JWT auth is enforced and `client_credentials` returns a token but catalog calls still answer `403`, the Keycloak service account for the chosen client is missing `gateway-backoffice` capability roles.
