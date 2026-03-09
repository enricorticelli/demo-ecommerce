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
2. Gli endpoint dipendono da command/query service applicativi, non da dettagli tecnici.
3. Ogni comando modifica stato di un solo bounded context.
4. Nessuna dipendenza diretta tra `Domain` di contesti diversi.
5. Separare `CommandService` e `QueryService` per responsabilita.
6. Spostare regole cross-entity in `Rules/Policy/Specification`, non negli endpoint.
7. Tenere il mapping `Entity -> View` in `Application` e `View -> Response` in mapper statici API.

## Regole infrastrutturali

1. `Program.cs` minimale: solo extension method di modulo (`Add...Module`, `Use...ModuleAsync`).
2. Repository in `Infrastructure`, interfacce in `Application`.
3. Publish eventi tramite astrazione `IDomainEventPublisher`; adapter tecnologico solo in `Infrastructure`.
4. Search/listing implementati in componenti query dedicati (query object/specification), non in service "fat".
5. Un tipo per file, evitare classi contenitore.

## Regole sui flussi cross-context

1. Integrare solo tramite contratti API/eventi.
2. Gestire sempre idempotenza su consumer asincroni.
3. Prevedere compensazioni per failure multi-step.

## Definition of Done tecnica

1. Invarianti di dominio testate.
2. Handler applicativi testati.
3. Endpoint con test integrazione minimi.
4. Nessun comportamento mock/stub nel percorso critico rilasciato.

## Convenzione naming test

Usare sempre il formato `Metodo_Scenario_ComportamentoAtteso` per i nomi dei test, in modo auto-descrittivo e ricercabile.

Esempi:

1. `GetBooks_EmptyDatabase_ReturnsEmptyList`
2. `Add_TwoPositiveNumbers_ReturnsSum`

## ADR correlate

- `../adr/0001-microservices-pragmatici.md`
- `../adr/0002-comunicazione-inter-context.md`
- `../adr/0003-data-ownership-database-separati.md`
- `../adr/0005-eventual-consistency-compensazioni.md`
- `../adr/0006-idempotenza-deduplica.md`
- `../adr/0008-strategia-test-backend.md`
- `./catalog-baseline-conventions.md`
