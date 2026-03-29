# Bounded Context: Order

## Purpose

Manage order lifecycle as the core process, from creation to completion/cancellation.

## Responsibilities

1. Create orders with domain invariants.
2. Manage order state transitions.
3. Coordinate cross-context process logic through events.

## Data ownership

- Order.
- Order status.
- Customer and address information associated with the order.

## Integrations

- Consumes events from Warehouse, Payment, and Shipping.
- Publishes events to Cart and other interested contexts.
- Exposes order query/command APIs in the `store` context.

## Boundaries

Order does not access external databases and does not embed payment/shipping internal rules.
