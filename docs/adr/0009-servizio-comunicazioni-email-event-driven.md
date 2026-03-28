# ADR-0009: Servizio Communication per email esterne event-driven

- Data: 2026-03-08
- Stato: Accepted
- Decisori: Product/Tech Owner
- Consultati: Stakeholder progetto
- Informati: Team backend/frontend

## Contesto

Serve introdurre comunicazioni utente (conferma ordine) senza accoppiare i bounded context core a provider email o chiamate sincrone tra servizi.

## Decisione

Adottare un nuovo bounded context `Communication` con consumo asincrono di eventi e invio email SMTP.

1. `Order` pubblica `OrderCompletedV1` con payload completo (incluso `CustomerEmail`).
2. `Communication` consuma l'evento e invia email tramite adapter SMTP.
3. Lo stesso evento puo essere consumato in parallelo da piu bounded context.
4. Idempotenza obbligatoria nei consumer con deduplica persistente.
5. Ambiente locale con Mailpit come mock SMTP e UI inbox.

## Alternative considerate

1. Invio email diretto da `Order`: coupling tecnico e responsabilita mescolate.
2. Chiamata HTTP da `Communication` verso altri context per recuperare email: coupling temporale e maggiore fragilita.
3. Provider email reale da subito: complessita operativa non necessaria in fase iniziale.

## Conseguenze

### Positive

- Separazione chiara tra dominio core e canale comunicazione.
- Resilienza grazie a integrazione asincrona e deduplica.
- Estendibilita verso nuovi template/canali senza toccare i servizi core.

### Negative / Trade-off

- Nuovo servizio da mantenere e monitorare.
- Overhead di un servizio dedicato e queue dedicate.

## Impatto su implementazione

- Contratto condiviso `OrderCompletedV1` riusato da piu consumer.
- Queue RabbitMQ: `order-completed-communication` (email) e altre queue consumer-specifiche sul fanout `order-completed`.
- Nuove configurazioni: `ConnectionStrings__CommunicationDb`, `Communication__Smtp__*`.
- Nuovo servizio `communication-api` e container `mailpit` nel `docker-compose`.

## Piano di adozione

1. Introdurre modulo `Communication` e wiring infrastrutturale.
2. Aggiornare producer `Order` per pubblicare un solo evento condiviso.
3. Aggiungere test handler/contratti e smoke test locale con Mailpit.

## Riferimenti

- `../architecture.md`
- `../bounded-contexts/communication.md`
- `../guidelines/integration-events.md`
