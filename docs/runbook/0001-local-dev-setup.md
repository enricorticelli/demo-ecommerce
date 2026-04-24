# Runbook: Start the Local Development Environment

## Prerequisites

- Docker Desktop running
- `.env` file present at repository root (copy `.env` if missing)

## Steps

### 1. Start infrastructure and all services

```bash
docker compose up --build
```

This starts: PostgreSQL, RabbitMQ, Aspire Dashboard, Mailpit, all API services, API Gateway, and the frontend.
It also starts Keycloak for local backoffice authentication and imports the `demo-ecommerce` realm.

### 2. Verify services are healthy

```bash
docker compose ps
```

All services should show `healthy` after the configured health check intervals.

### 3. Access endpoints

| Service            | URL                                         |
|--------------------|---------------------------------------------|
| API Gateway        | http://localhost:${GATEWAY_HOST_PORT}        |
| Frontend (web)     | http://localhost:${FRONTEND_WEB_HOST_PORT}   |
| Backoffice (auto)  | http://localhost:${BACKOFFICE_WEB_HOST_PORT} |
| RabbitMQ UI        | http://localhost:${RABBITMQ_MANAGEMENT_HOST_PORT} |
| Keycloak Admin     | http://localhost:${KEYCLOAK_HOST_PORT}             |
| Aspire Dashboard   | http://localhost:${ASPIRE_DASHBOARD_HOST_PORT} |
| Mailpit UI         | http://localhost:${MAILPIT_UI_HOST_PORT}     |
| OpenAPI (Gateway)  | http://localhost:${GATEWAY_HOST_PORT}/openapi/v1.json |

### Backoffice authentication

Keycloak owns backoffice users and capability assignments. Do not put application users in repository config files.

Local realm import creates:
- realm: `demo-ecommerce`
- public client: `backoffice-web`
- confidential resource client: `gateway-backoffice`
- capability roles on `gateway-backoffice`

Create local users from the Keycloak Admin UI, then assign the required `gateway-backoffice` client roles. The Gateway reads capabilities only through Keycloak token introspection; clients must not rely on token contents.

For local development, the Gateway introspection client secret defaults to `GATEWAY_BACKOFFICE_CLIENT_SECRET`. In production, provide it via environment variables or a secret manager.

Default port values are in `.env`.

### 4. Stop

```bash
docker compose down
```

To also remove volumes (clears database data):
```bash
docker compose down -v
```

## Troubleshooting

- **PostgreSQL not ready**: Wait for the `pg_isready` health check to pass (up to 30 s, 3 retries).
- **RabbitMQ queues missing**: Wolverine `AutoProvision()` creates queues on first connection. Restart the affected service if queues are absent.
- **Frontend hot-reload not working**: Ensure `CHOKIDAR_USEPOLLING=true` is set in `.env` (required on Windows).
