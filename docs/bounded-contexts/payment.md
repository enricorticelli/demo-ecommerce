# Bounded Context: Payment

## Scopo

Gestire il ciclo di vita del pagamento dal momento in cui nasce un ordine fino alla notifica di esito verso `Order`, mantenendo ownership su sessione, stato e integrazione con provider esterni.

## Responsabilita

1. Creare/aggiornare la `PaymentSession` quando arriva `OrderCreatedV1`.
2. Preparare checkout hosted lato provider (mock) su richiesta del frontend.
3. Validare e processare webhook provider con deduplica.
4. Applicare transizione di stato sessione (`Pending` -> `Authorized`/`Rejected`).
5. Pubblicare eventi integrazione `PaymentAuthorizedV1` e `PaymentRejectedV1`.

## Ownership dati

Payment possiede i dati in schema `payment`:

- `payment.payment_sessions`:
  - identificativi: `SessionId`, `OrderId`, `UserId`
  - checkout/provider: `PaymentMethod`, `ProviderCode`, `ExternalCheckoutId`, `ProviderStatus`, `RedirectUrl`
  - stato dominio: `Status`, `TransactionId`, `FailureReason`
  - diagnostica webhook: `LastWebhookEventId`, `LastProviderPayload`, `LastWebhookReceivedAtUtc`
  - auditing: `CreatedAtUtc`, `CompletedAtUtc`
- `payment.processed_integration_events`: deduplica eventi in ingresso (es. `OrderCreatedV1`).
- `payment.processed_webhook_events`: deduplica webhook provider per chiave `(ProviderCode, ExternalEventId)`.

Vincoli principali:

1. `OrderId` univoco in `payment_sessions` (una sessione per ordine).
2. `ExternalCheckoutId` indicizzato per lookup webhook.
3. `FailureReason` max 256, `LastProviderPayload` max 4096.

## Modello dominio

Entita root: `PaymentSession` (`src/Payment.Domain/Entities/PaymentSession.cs`).

Stato sessione:

1. `Pending`: default alla creazione.
2. `Authorized`: pagamento autorizzato, `TransactionId` valorizzato, `CompletedAtUtc` valorizzato.
3. `Rejected`: pagamento rifiutato/annullato, `FailureReason` valorizzata, `CompletedAtUtc` valorizzato.

Regole principali:

1. `Amount` non puo essere negativo.
2. `PaymentMethod` di default: `stripe_card` se assente.
3. Cambio `PaymentMethod` resetta contesto provider (`ProviderCode`, `ExternalCheckoutId`, `ProviderStatus`, `RedirectUrl`).
4. `Authorize()` e `Reject()` sono idempotenti: se stato gia finale uguale, non fanno update.

## Flussi applicativi

### 1) Inizializzazione sessione da evento ordine

Handler: `AuthorizePaymentOnOrderCreatedHandler` (`src/Payment.Application/Handlers/AuthorizePaymentOnOrderCreatedHandler.cs`).

Input:

- evento `OrderCreatedV1` (da exchange `order-created`, queue `order-created-payment`).

Comportamento:

1. Deduplica evento con `IPaymentEventDeduplicationStore`.
2. Se non esiste sessione per `OrderId`, crea `PaymentSession` con `UserId`, `TotalAmount`, `PaymentMethod` ricevuti.
3. Se esiste gia, aggiorna il checkout context (`UserId`, `Amount`, `PaymentMethod`).
4. Salva su DB.

Output:

- Nessun evento in uscita in questa fase.

### 2) Creazione/recupero checkout hosted

Endpoint query sessione ordine:

- `GET /api/store/payment/v1/payments/sessions/orders/{orderId}`

Servizio: `PaymentQueryService` -> `PaymentCheckoutService`.

Comportamento:

1. Cerca sessione per `OrderId`; se assente ritorna `404`.
2. Se sessione non `Pending`, ritorna stato attuale.
3. Se `Pending` ma checkout provider gia configurato (`RedirectUrl`, `ExternalCheckoutId`, `ProviderCode`), ritorna dati esistenti.
4. Altrimenti:
	- risolve provider da `PaymentMethod` (`IPaymentProviderRouter.ResolveByPaymentMethod`)
	- invoca `CreateCheckoutAsync` su provider adapter
	- persiste `ProviderCode`, `ExternalCheckoutId`, `RedirectUrl`, `ProviderStatus`
	- ritorna sessione aggiornata

Effetto:

- front-end riceve `RedirectUrl` verso hosted checkout (mock gateway).

### 3) Processamento webhook provider

Endpoint webhook:

1. `POST /api/store/payment/v1/payments/webhooks/stripe`
2. `POST /api/store/payment/v1/payments/webhooks/paypal`
3. `POST /api/store/payment/v1/payments/webhooks/satispay`

Servizio: `PaymentWebhookService`.

Pipeline:

1. Legge raw payload e header.
2. Verifica firma con `IPaymentWebhookVerifier` (header `X-Mock-Signature`, HMAC SHA256 con `WebhookSecret`).
3. Parsifica payload provider in `PaymentWebhookNotification`.
4. Valida `ExternalEventId`.
5. Deduplica webhook (`providerCode` + `externalEventId`).
6. Lookup sessione:
	- prima per `SessionId` (se presente)
	- fallback per `ExternalCheckoutId`
7. Se sessione non trovata: marca comunque webhook come processato e ritorna `SessionNotFound`.
8. Aggiorna metadati provider su sessione (`ProviderCode`, `ExternalCheckoutId`, `ProviderStatus`, ultimo payload/evento/timestamp).
9. Se decisione webhook e `Authorized`, chiama `AuthorizeAsync`.
10. Se decisione webhook e `Rejected`, chiama `RejectAsync`.
11. Marca webhook processato.

Risposte endpoint:

1. `401` per firma invalida.
2. `400` per payload invalido.
3. `200` per `Processed`, `Duplicate`, `SessionNotFound`.

### 4) Cambio stato e pubblicazione eventi

Servizio: `PaymentCommandService`.

1. `AuthorizeAsync(sessionId, correlationId, transactionId)`:
	- applica transizione su sessione
	- se lo stato cambia, pubblica `PaymentAuthorizedV1(orderId, transactionId, metadata)`
2. `RejectAsync(sessionId, reason, correlationId)`:
	- normalizza/sanitizza reason
	- applica transizione su sessione
	- se lo stato cambia, pubblica `PaymentRejectedV1(orderId, reason, metadata)`

Nota: `reason` viene sanitizzata rimuovendo sequenze numeriche 12-19 cifre e troncata a 256 caratteri.

## Integrazione provider

Router provider: `PaymentProviderRouter`.

Supporto metodi pagamento:

1. Stripe adapter:
	- metodi: `stripe_card`, `stripecard`, `card`, `creditcard`
	- provider code: `stripe`
2. PayPal adapter:
	- metodi: `paypal`
	- provider code: `paypal`
3. Satispay adapter:
	- metodi: `satispay`
	- provider code: `satispay`

Mapping status webhook -> decisione dominio:

1. Stripe:
	- `checkout.session.completed`, `payment_intent.succeeded` -> `Authorized`
	- `checkout.session.expired`, `checkout.session.async_payment_failed`, `payment_intent.payment_failed` -> `Rejected`
2. PayPal:
	- `CHECKOUT.ORDER.APPROVED`, `PAYMENT.CAPTURE.COMPLETED` -> `Authorized`
	- `CHECKOUT.ORDER.CANCELLED`, `CHECKOUT.ORDER.VOIDED`, `PAYMENT.CAPTURE.DENIED` -> `Rejected`
3. Satispay:
	- `ACCEPTED` -> `Authorized`
	- `CANCELED`, `CANCELLED`, `REJECTED` -> `Rejected`

Status non riconosciuti restano `Pending` (nessuna transizione finale).

## Eventi di integrazione

Eventi consumati:

1. `OrderCreatedV1`.

Eventi pubblicati:

1. `PaymentAuthorizedV1`:
	- campi: `OrderId`, `TransactionId`, `Metadata`
2. `PaymentRejectedV1`:
	- campi: `OrderId`, `Reason`, `Metadata`

Metadata evento creati con `IntegrationEventMetadataFactory` (`CorrelationId`, timestamp UTC, source context `Payment`).

Trasporto messaggi (Wolverine + RabbitMQ):

1. subscribe queue `order-created-payment`
2. publish queue `payment-authorized-order`
3. publish queue `payment-rejected-order`

## Dipendenze cross-context

Payment non modifica mai lo stato ordine direttamente.

`Order` consuma gli eventi Payment:

1. `HandlePaymentAuthorizedOnOrderHandler` applica pagamento su ordine e puo emettere `OrderCompletedV1`.
2. `HandlePaymentRejectedOnOrderHandler` applica rifiuto e puo emettere `OrderCancelledV1`.

## Configurazione operativa

Chiavi principali:

1. `ConnectionStrings__PaymentDb` (o `ConnectionStrings:PaymentDb`)
2. `MessageBus__Host`, `MessageBus__Port`, `MessageBus__Username`, `MessageBus__Password`
3. `Payment:SkipWolverineBootstrap` / `Payment:SkipDatabaseBootstrap`
4. Provider options:
	- `Payment:Providers:Stripe:*`
	- `Payment:Providers:PayPal:*`
	- `Payment:Providers:Satispay:*`
	- campi: `CheckoutApiBaseUrl`, `ApiKey`, `WebhookSecret`, `ReturnUrlTemplate`, `CancelUrlTemplate`

Default return/cancel URL template puntano a `http://localhost:3000/payment/return`.

## Mock gateway (sviluppo)

Servizio: `frontend/payment-mock-gateway/server.js`.

Funzioni:

1. Espone `POST /mock/{provider}/checkouts` per creare checkout mock.
2. Espone pagina hosted `GET /checkout/{provider}/{checkoutId}` con decisione utente.
3. Invia webhook firmato verso Gateway API:
	- `POST /api/store/payment/v1/payments/webhooks/{provider}`
	- header `X-Mock-Signature: sha256=<hmac>`
4. Reindirizza utente a `returnUrl` o `cancelUrl` con query di esito.

## Confini

Payment resta owner esclusivo di sessione e stato pagamento.

Non compete a Payment:

1. decidere stato finale business dell'ordine
2. accedere a DB di altri bounded context
3. applicare regole di fulfillment/shipping

La comunicazione cross-context avviene solo via eventi contrattualizzati.
