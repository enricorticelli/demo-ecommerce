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
| RabbitMQ UI        | http://localhost:${RABBITMQ_MANAGEMENT_HOST_PORT} |
| Aspire Dashboard   | http://localhost:${ASPIRE_DASHBOARD_HOST_PORT} |
| Mailpit UI         | http://localhost:${MAILPIT_UI_HOST_PORT}     |
| OpenAPI (Gateway)  | http://localhost:${GATEWAY_HOST_PORT}/openapi/v1.json |

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
