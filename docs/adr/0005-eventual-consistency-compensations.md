# ADR-0005: Eventual Consistency and Compensation via Event Choreography

- Status: accepted
- Date: 2026-04-22

## Context

The order fulfilment flow spans multiple bounded contexts (Order, Payment, Warehouse). A distributed transaction (2PC/saga orchestrator) would require tight coupling. The system instead tolerates eventual consistency.

## Decision

Order fulfilment uses **event choreography**: each context reacts to events from others and emits its own.

Flow:
1. `Order` created → publishes `OrderCreatedV1` → triggers Payment and Warehouse in parallel.
2. `Payment` authorises → publishes `PaymentAuthorizedV1` → `Order` applies `ApplyPaymentAuthorized()`.
3. `Warehouse` reserves stock → publishes `StockReservedV1` → `Order` applies `ApplyStockReserved()`.
4. When both flags are set, `Order.TryComplete()` transitions the order to `Completed` and publishes `OrderCompletedV1`.
5. On rejection (`PaymentRejectedV1` / `StockRejectedV1`), `Order` cancels and triggers compensations.

Evidence: `src/Order.Domain/Entities/Order.cs` lines 172–219, `src/Order.Application/Handlers/`.

## Consequences

- No orchestrator service required.
- Partial failures leave the order in `Pending` until compensated.
- New fulfilment steps require new event pairs and a new flag on `Order`.
