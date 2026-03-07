# ADR-0006: Idempotenza handler e deduplica dei messaggi

- Data: 2026-03-07
- Stato: Accepted
- Decisori: Product/Tech Owner
- Consultati: Stakeholder progetto
- Informati: Team backend/frontend

## Contesto

Con integrazione event-driven, la consegna dei messaggi puo causare retry e duplicati. Senza idempotenza, i consumer possono applicare la stessa transizione piu volte, introducendo bug di dominio.

## Decisione

Rendere idempotenti tutti i consumer asincroni e introdurre deduplica:

1. ogni handler deve tollerare duplicati dello stesso evento;
2. usare chiavi di deduplica (`eventId`, `correlationId`) per tracciamento;
3. trattare il reorder dei messaggi come caso previsto;
4. definire policy di replay sicure.

## Alternative considerate

1. Affidarsi solo al broker: non elimina duplicati a livello applicativo.
2. Idempotenza solo sui flussi principali: lascia zone fragili e non prevedibili.
3. Compensazioni senza idempotenza: insufficiente e costoso da gestire.

## Conseguenze

### Positive

- Riduzione effetti collaterali da retry/duplicati.
- Maggiore affidabilita dei workflow distribuiti.
- Facilita di recovery e replay controllato.

### Negative / Trade-off

- Piu complessita nei consumer.
- Necessita di storage e policy di deduplica.

## Impatto su implementazione

- Definire standard tecnici per check idempotenza in ogni handler.
- Introdurre logiche di upsert/compare-and-set dove richiesto.
- Aggiungere test specifici su duplicati e out-of-order events.

## Piano di adozione

1. Introdurre guideline tecniche comuni per consumer.
2. Adeguare progressivamente i workflow piu critici.
3. Verificare in test integrazione i casi di replay e duplicazione.

## Riferimenti

- `./0002-comunicazione-inter-context.md`
- `./0005-eventual-consistency-compensazioni.md`
- `../guidelines/integration-events.md`
