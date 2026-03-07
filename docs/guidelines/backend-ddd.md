# Guideline Backend DDD Tattico

Queste linee guida traducono gli ADR in regole implementative per il codice backend.

## Obiettivo

Implementare logica di business reale nei layer `Application` e `Domain`, mantenendo `Api` come puro adattatore.

## Regole di modellazione

1. `Entity`: usare quando esiste identita e lifecycle.
2. `Value Object`: usare per concetti immutabili con uguaglianza per valore.
3. `Aggregate Root`: una sola root per boundary transazionale.
4. `Domain Service`: solo per regole che non appartengono chiaramente a un aggregate.
5. `Repository`: interfaccia in `Application` o `Domain`, implementazione in `Infrastructure`.

## Regole applicative

1. Gli endpoint non contengono business logic.
2. Gli application service/handler orchestrano, il dominio decide.
3. Ogni comando modifica stato di un solo bounded context.
4. Nessuna dipendenza diretta tra `Domain` di contesti diversi.

## Regole sui flussi cross-context

1. Integrare solo tramite contratti API/eventi.
2. Gestire sempre idempotenza su consumer asincroni.
3. Prevedere compensazioni per failure multi-step.

## Definition of Done tecnica

1. Invarianti di dominio testate.
2. Handler applicativi testati.
3. Endpoint con test integrazione minimi.
4. Nessun comportamento mock/stub nel percorso critico rilasciato.

## ADR correlate

- `../adr/0001-microservices-pragmatici.md`
- `../adr/0002-comunicazione-inter-context.md`
- `../adr/0003-data-ownership-database-separati.md`
- `../adr/0005-eventual-consistency-compensazioni.md`
- `../adr/0006-idempotenza-deduplica.md`
- `../adr/0008-strategia-test-backend.md`
