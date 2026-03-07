# Agent Guidelines (Single Source)

Questo documento e la fonte unica per le regole operative di agent automatici (Codex/Copilot/assistenti) in questo repository.

## Ordine di precedenza
In caso di conflitto tra documenti, prevale questo ordine:
1. `docs/technical-guidelines.md`
2. `docs/architecture.md`
3. `docs/agent-guidelines.md`
4. altri documenti istruttivi di supporto

## Obiettivo e principi
- Qualita prima della velocita.
- Correttezza funzionale e robustezza sugli edge case.
- Coerenza architetturale end-to-end tra microservizi e frontend.
- Nessun workaround che violi layering, SOLID o contratti API/evento.

## Vincoli architetturali globali
Per tutti i moduli backend (`Catalog`, `Warehouse`, `Shipping`, `Order`, `Cart`, `User`, `Payment`) valgono:
- CQRS puro.
- Event Sourcing dove previsto dal dominio (obbligatorio almeno per `Cart` e `Order`).
- Clean Architecture con separazione netta dei layer.
- SOLID applicato in modo pratico.

Non e consentito introdurre endpoint o logica business che bypassino questi principi.

## Layering obbligatorio per microservizio
Struttura:
- `*.Api`: endpoint Minimal API, mapping request/response, wiring.
- `*.Application`: command/query CQRS, handler, orchestrazione, contratti applicativi.
- `*.Domain`: aggregate, invarianti, value object, eventi dominio.
- `*.Infrastructure`: persistenza/event bus (Marten, Wolverine), adapter esterni, implementazioni concrete.

Regole hard:
- `Domain` non dipende da framework IO.
- `Application` dipende da astrazioni, non da implementazioni concrete.
- `Api` non contiene business logic.
- Marten/Wolverine solo in `Infrastructure`.

## CQRS ed endpoint conventions
- Ogni scrittura: `Command` + `ICommandHandler` + dispatcher.
- Ogni lettura: `Query` + `IQueryHandler` + dispatcher.
- Endpoint sottili: delegano al dispatcher.
- Nessun accesso diretto a implementazioni di dominio dagli endpoint.
- Route versionate `/v1/...`.
- Usare `TypedResults` e `ProblemDetails` (RFC 7807).
- Naming coerente endpoint (`WithName`, `WithTags`).
- Validazione input al boundary API.

## Mapping conventions
- Il mapping `request -> command/payload` e `view/model -> response` deve stare in classi statiche dedicate `*Mapper.cs`.
- Evitare mapping inline complesso negli endpoint.
- Mantenere i mapper nel progetto `*.Api` vicino agli endpoint (es. cartella `Endpoints/`).

Pattern contratti API (obbligatori):
- Strutturare i contratti in:
  - `Contracts/Requests/*.cs`
  - `Contracts/Responses/*.cs`
- Namespace coerente del modulo (es. `Order.Api.Contracts`).
- Endpoint con `Request`/`Response` tipizzati (`TypedResults`).
- I mapper statici `*Mapper.cs` devono coprire:
  - `Request -> Command/Integration Payload`
  - `View/Model -> Response`
- Vietato duplicare mapping equivalenti in endpoint diversi dello stesso modulo.
- Vietato introdurre logica di dominio nei mapper.

## Event-driven e integrazione
- Comunicazione inter-servizio nel core workflow tramite RabbitMQ/Wolverine.
- Niente HTTP service-to-service nei flussi core di checkout.
- Eventi/contratti integrazione versionati (`*V1`).
- Handler idempotenti e tolleranti a duplicati.
- Stati terminali (`Completed`/`Failed`) non devono regredire salvo regola dominio esplicita.

## Frontend e backoffice
- Web pubblico e backoffice restano separati.
- Ogni modifica backend che tocca contratti pubblici deve verificare compatibilita con:
  - `frontend/web/src/lib/api.ts`
  - `frontend/admin/src/lib/api.ts`
- Mantenere UX operativa chiara e coerenza visuale con pattern esistenti.

## Testing, build e Definition of Done
Ogni modifica rilevante deve includere o aggiornare test pertinenti:
- Unit test per handler/validator/invarianti.
- Integration test per endpoint e flussi publish/consume critici.

DoD minima:
- Build backend verde.
- Build frontend web e admin verde.
- Nessun errore analizzatori rilevante.
- Contratti API/evento coerenti con i consumer.
- Documentazione aggiornata quando cambia comportamento pubblico o architettura.

## Sicurezza e operativita
- Nessun segreto in codice o log.
- Configurazione via env.
- Logging strutturato con correlation context.
- Non rompere il bootstrap docker-first (`docker compose up --build`).

## Regole operative per agent
Quando implementi:
- Applica modifiche minime ma complete end-to-end.
- Non introdurre dipendenze nuove senza necessita forte.
- Non rompere i confini tra bounded context.
- Se la richiesta utente viola queste linee, proporre alternativa compatibile.
- Non fare refactor non richiesti fuori scope.

## Checklist rapida prima di chiudere
- Ho rispettato i confini `Api/Application/Domain/Infrastructure`.
- Ho mantenuto CQRS puro (command/query + dispatcher).
- Ho messo mapping request/response in `*Mapper.cs`.
- Ho mantenuto separazione `Contracts/Requests` e `Contracts/Responses`.
- Ho verificato contratti usati da web e backoffice.
- Ho eseguito build/test disponibili e riportato eventuali limiti ambientali.

## Lettura obbligatoria prima di modificare codice
Prima di iniziare qualsiasi modifica, leggere almeno:
- `docs/technical-guidelines.md`
- `docs/architecture.md`
- `docs/api-patterns.md`
- `docs/checkout-flow.md` (se tocchi il workflow di checkout)
- `docs/adr/*.md` pertinenti all'area modificata
