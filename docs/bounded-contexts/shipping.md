# Bounded Context: Shipping

## Purpose

Manage shipment creation and logistics status progression.

## Responsibilities

1. Create shipments after process triggers.
2. Generate tracking codes.
3. Update shipment status over time.

## Data ownership

- Shipment.
- Tracking code.
- Delivery status.

## Integrations

- Consumes creation requests from Order.
- Publishes outcomes/updates to Order.
- Exposes shipment status query APIs in the `store` context.

## Boundaries

Shipping does not handle payment or stock business logic.
