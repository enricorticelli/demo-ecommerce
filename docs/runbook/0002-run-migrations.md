# Runbook: Apply EF Core Database Migrations

Each bounded context manages its own migrations in `[Context].Infrastructure/Persistence/Migrations/`.

## Prerequisites

- PostgreSQL container running (`docker compose up postgres -d`)
- `dotnet` CLI installed (SDK 10.0)
- `ConnectionStrings__[Context]Db` environment variable set (or `.env` sourced)

## Apply migrations for a single context

```bash
cd src/[Context].Infrastructure
dotnet ef database update \
  --startup-project ../[Context].Api/[Context].Api.csproj
```

## Create a new migration

```bash
cd src/[Context].Infrastructure
dotnet ef migrations add [MigrationName] \
  --startup-project ../[Context].Api/[Context].Api.csproj \
  --output-dir Persistence/Migrations
```

## Apply all contexts in order (development)

Run the following for each context (Catalog, Cart, Order, Payment, Warehouse, Shipping, Communication):

```bash
for ctx in Catalog Cart Order Payment Warehouse Shipping Communication; do
  dotnet ef database update \
    --project src/${ctx}.Infrastructure/${ctx}.Infrastructure.csproj \
    --startup-project src/${ctx}.Api/${ctx}.Api.csproj
done
```

## Notes

- Migrations run automatically on service startup if configured (verify `MigrateAsync` call in infrastructure bootstrap).
- The `init-multiple-databases.sh` script (`scripts/postgres/init-multiple-databases.sh`) only creates databases; it does not apply EF Core schema migrations.
- Use `dotnet ef migrations list` to inspect the current state.
