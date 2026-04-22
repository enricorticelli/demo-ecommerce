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

## Environment overrides

| Variable           | Default                          | Purpose                          |
|--------------------|----------------------------------|----------------------------------|
| `PUBLIC_GATEWAY_URL` | Read from `.env`               | Base URL for Gateway API         |
| `SEED_CONCURRENCY` | 5                                | Parallel request pool size       |
| `SEED_TIMEOUT_MS`  | 10000                            | Per-request timeout (ms)         |

## Notes

- The script is idempotent: it deletes existing data before seeding.
- Retry logic uses exponential backoff (see `scripts/seeding/seed-catalog.js` lines 234–291).
- Correlation IDs are logged per batch for debugging.
