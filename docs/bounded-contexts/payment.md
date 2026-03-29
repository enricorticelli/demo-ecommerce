# Bounded Context: Payment

## Purpose

Manage the payment lifecycle from order creation to outcome notification toward `Order`, owning session, state, and provider integrations.

## Responsibilities

1. Create/update `PaymentSession` when `OrderCreatedV1` arrives.
2. Prepare hosted checkout through provider (mock) on frontend demand.
3. Validate and process provider webhooks with deduplication.
4. Apply payment session state transitions (`Pending` -> `Authorized`/`Rejected`).
5. Publish integration events `PaymentAuthorizedV1` and `PaymentRejectedV1`.

## Data ownership

Payment owns data in `payment` schema:

- `payment.payment_sessions`:
  - identifiers: `SessionId`, `OrderId`, `UserId`
  - checkout/provider: `PaymentMethod`, `ProviderCode`, `ExternalCheckoutId`, `ProviderStatus`, `RedirectUrl`
  - domain state: `Status`, `TransactionId`, `FailureReason`
  - webhook diagnostics: `LastWebhookEventId`, `LastProviderPayload`, `LastWebhookReceivedAtUtc`
  - auditing: `CreatedAtUtc`, `CompletedAtUtc`
- `payment.processed_integration_events`: deduplication for incoming integration events (for example `OrderCreatedV1`).
- `payment.processed_webhook_events`: webhook deduplication by key `(ProviderCode, ExternalEventId)`.

Main constraints:

1. `OrderId` unique in `payment_sessions` (one session per order).
2. `ExternalCheckoutId` indexed for webhook lookup.
3. `FailureReason` max 256, `LastProviderPayload` max 4096.

## Domain model

Root entity: `PaymentSession` (`src/Payment.Domain/Entities/PaymentSession.cs`).

Session states:

1. `Pending`: default at creation.
2. `Authorized`: payment authorized, `TransactionId` set, `CompletedAtUtc` set.
3. `Rejected`: payment rejected/cancelled, `FailureReason` set, `CompletedAtUtc` set.

Main rules:

1. `Amount` cannot be negative.
2. Default `PaymentMethod`: `stripe_card` when missing.
3. Changing `PaymentMethod` resets provider context (`ProviderCode`, `ExternalCheckoutId`, `ProviderStatus`, `RedirectUrl`).
4. `Authorize()` and `Reject()` are idempotent: no update when already in same terminal state.

## Application flows

### 1) Session initialization from order event

Handler: `AuthorizePaymentOnOrderCreatedHandler` (`src/Payment.Application/Handlers/AuthorizePaymentOnOrderCreatedHandler.cs`).

Input:

- `OrderCreatedV1` event (from `order-created` exchange, `order-created-payment` queue).

Behavior:

1. Deduplicate event via `IPaymentEventDeduplicationStore`.
2. If no session exists for `OrderId`, create `PaymentSession` with incoming `UserId`, `TotalAmount`, `PaymentMethod`.
3. If it exists, update checkout context (`UserId`, `Amount`, `PaymentMethod`).
4. Persist in DB.

Output:

- No outgoing integration event in this phase.

### 2) Hosted checkout create/retrieve

Order/session query endpoints:

- `GET /api/store/payment/v1/payments/sessions/orders/{orderId}`
- `GET /api/store/payment/v1/payments/sessions/{sessionId}`

Service: `PaymentQueryService` -> `PaymentCheckoutService`.

Behavior:

1. Find session by `OrderId`; return `404` if missing.
2. If session is not `Pending`, return current state.
3. If `Pending` but provider checkout is already configured (`RedirectUrl`, `ExternalCheckoutId`, `ProviderCode`), return existing data.
4. Otherwise:
   - resolve provider from `PaymentMethod` (`IPaymentProviderRouter.ResolveByPaymentMethod`)
   - call `CreateCheckoutAsync` on provider adapter
   - persist `ProviderCode`, `ExternalCheckoutId`, `RedirectUrl`, `ProviderStatus`
   - return updated session

Effect:

- frontend receives `RedirectUrl` to hosted checkout (mock gateway).

### 3) Provider webhook processing

Webhook endpoints:

1. `POST /api/store/payment/v1/payments/webhooks/stripe`
2. `POST /api/store/payment/v1/payments/webhooks/paypal`
3. `POST /api/store/payment/v1/payments/webhooks/satispay`

Context note:

1. Session and webhook endpoints are exposed in `store` to support storefront checkout and provider callbacks.

Service: `PaymentWebhookService`.

Pipeline:

1. Read raw payload and headers.
2. Verify signature via `IPaymentWebhookVerifier` (`X-Mock-Signature` header, HMAC SHA256 with `WebhookSecret`).
3. Parse provider payload into `PaymentWebhookNotification`.
4. Validate `ExternalEventId`.
5. Deduplicate webhook (`providerCode` + `externalEventId`).
6. Session lookup:
   - first by `SessionId` (if present)
   - fallback by `ExternalCheckoutId`
7. If session is not found: still mark webhook processed and return `SessionNotFound`.
8. Update provider metadata on session (`ProviderCode`, `ExternalCheckoutId`, `ProviderStatus`, last payload/event/timestamp).
9. If webhook decision is `Authorized`, call `AuthorizeAsync`.
10. If webhook decision is `Rejected`, call `RejectAsync`.
11. Mark webhook processed.

Endpoint responses:

1. `401` for invalid signature.
2. `400` for invalid payload.
3. `200` for `Processed`, `Duplicate`, `SessionNotFound`.

### 4) State transition and event publication

Service: `PaymentCommandService`.

1. `AuthorizeAsync(sessionId, correlationId, transactionId)`:
   - apply session transition
   - if state changes, publish `PaymentAuthorizedV1(orderId, transactionId, metadata)`
2. `RejectAsync(sessionId, reason, correlationId)`:
   - normalize/sanitize reason
   - apply session transition
   - if state changes, publish `PaymentRejectedV1(orderId, reason, metadata)`

Note: `reason` is sanitized by removing 12-19 digit numeric sequences and truncated to 256 characters.

## Provider integration

Provider router: `PaymentProviderRouter`.

Supported payment methods:

1. Stripe adapter:
   - methods: `stripe_card`, `stripecard`, `card`, `creditcard`
   - provider code: `stripe`
2. PayPal adapter:
   - methods: `paypal`
   - provider code: `paypal`
3. Satispay adapter:
   - methods: `satispay`
   - provider code: `satispay`

Webhook status -> domain decision mapping:

1. Stripe:
   - `checkout.session.completed`, `payment_intent.succeeded` -> `Authorized`
   - `checkout.session.expired`, `checkout.session.async_payment_failed`, `payment_intent.payment_failed` -> `Rejected`
2. PayPal:
   - `CHECKOUT.ORDER.APPROVED`, `PAYMENT.CAPTURE.COMPLETED` -> `Authorized`
   - `CHECKOUT.ORDER.CANCELLED`, `CHECKOUT.ORDER.VOIDED`, `PAYMENT.CAPTURE.DENIED` -> `Rejected`
3. Satispay:
   - `ACCEPTED` -> `Authorized`
   - `CANCELED`, `CANCELLED`, `REJECTED` -> `Rejected`

Unknown statuses remain `Pending` (no terminal transition).

## Integration events

Consumed events:

1. `OrderCreatedV1`.

Published events:

1. `PaymentAuthorizedV1`:
   - fields: `OrderId`, `TransactionId`, `Metadata`
2. `PaymentRejectedV1`:
   - fields: `OrderId`, `Reason`, `Metadata`

Event metadata is created through `IntegrationEventMetadataFactory` (`CorrelationId`, UTC timestamp, source context `Payment`).

Message transport (Wolverine + RabbitMQ):

1. subscribe queue `order-created-payment`
2. publish queue `payment-authorized-order`
3. publish queue `payment-rejected-order`

## Cross-context dependencies

Payment never updates order state directly.

`Order` consumes Payment events:

1. `HandlePaymentAuthorizedOnOrderHandler` applies payment to order and may emit `OrderCompletedV1`.
2. `HandlePaymentRejectedOnOrderHandler` applies rejection and may emit `OrderCancelledV1`.

## Operational configuration

Main keys:

1. `ConnectionStrings__PaymentDb` (or `ConnectionStrings:PaymentDb`)
2. `MessageBus__Host`, `MessageBus__Port`, `MessageBus__Username`, `MessageBus__Password`
3. `Payment:SkipWolverineBootstrap` / `Payment:SkipDatabaseBootstrap`
4. Provider options:
   - `Payment:Providers:Stripe:*`
   - `Payment:Providers:PayPal:*`
   - `Payment:Providers:Satispay:*`
   - fields: `CheckoutApiBaseUrl`, `ApiKey`, `WebhookSecret`, `ReturnUrlTemplate`, `CancelUrlTemplate`

Default return/cancel URL templates point to `http://localhost:3000/payment/return`.

## Mock gateway (development)

Service: `frontend/payment-mock-gateway/server.js`.

Functions:

1. Exposes `POST /mock/{provider}/checkouts` to create mock checkouts.
2. Exposes hosted page `GET /checkout/{provider}/{checkoutId}` for user decision.
3. Sends signed webhook to Gateway API:
   - `POST /api/store/payment/v1/payments/webhooks/{provider}`
   - header `X-Mock-Signature: sha256=<hmac>`
4. Redirects user to `returnUrl` or `cancelUrl` with outcome query params.

## Boundaries

Payment remains exclusive owner of payment session and payment state.

Payment does not:

1. decide final business state of the order;
2. access other bounded context databases;
3. apply fulfillment/shipping business rules.

Cross-context communication happens only through contract-based integration events.
