# Bounded Context: Communication

## Purpose

Handle asynchronous external communications to end users via email.

## Responsibilities

1. Consume integration events relevant for user notifications.
2. Compose and send transactional emails (for example order confirmation, order shipped).
3. Guarantee consumer idempotency to prevent duplicate sends.

## Data ownership

- Processed integration events (deduplication).
- Technical email channel configuration (SMTP).

## Integrations

- Consumes events from `Order` and `Shipping`.
- Sends emails via SMTP to external provider/mock (Mailpit locally).

## Boundaries

Communication does not change Order/Shipping state and does not access their databases.
