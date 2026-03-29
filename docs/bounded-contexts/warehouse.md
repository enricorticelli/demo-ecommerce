# Bounded Context: Warehouse

## Purpose

Manage stock availability and reservation for orders.

## Responsibilities

1. Update inventory availability.
2. Reserve stock for order workflows.
3. Emit reservation outcome (`Reserved`/`Rejected`).

## Data ownership

- Stock by product.
- Stock reservations.

## Integrations

- Consumes reservation requests from the order process.
- Publishes outcomes to Order.
- Exposes technical stock management APIs in the `backoffice` context.

## Public endpoints (summary)

- `POST /api/backoffice/warehouse/v1/stock/query`
- `POST /api/backoffice/warehouse/v1/stock`

## Boundaries

Warehouse does not modify orders and does not access payment/shipping data.
