# Bounded Context: Cart

## Purpose

Manage the user cart up to checkout.

## Responsibilities

1. Add/remove items.
2. Calculate cart total.
3. Produce data required to start order creation.

## Data ownership

- Cart.
- Cart items.
- Context-local checkout state.

## Integrations

- Consumes product data needed by the cart through contracts.
- Exposes frontend APIs in the `store` context.
- Publishes checkout-related events.

## Boundaries

Cart does not modify orders or payments directly; it only emits intents/events.
