# Copilot Instructions - CQRS E-commerce

Queste istruzioni sono vincolanti per qualsiasi modifica al repository.
In caso di conflitto, prevalgono: `docs/technical-guidelines.md`, `docs/architecture.md`, `AGENTS.md`.

## 1) Principi non negoziabili
- Qualita prima della velocita.
- Correttezza funzionale, robustezza sugli edge case, manutenibilita.
- Coerenza architetturale tra tutti i moduli.
- Nessun workaround che violi layering, SOLID o contratti di integrazione.

## 2) Vincolo architetturale globale
In tutti i moduli backend (`Catalog`, `Warehouse`, `Shipping`, `Order`, `Cart`, `User`, `Payment`) devono essere rispettati:
- CQRS puro.
- Event Sourcing dove previsto dal dominio e dal flusso (obbligatorio almeno per `Cart` e `Order`, estendibile ai moduli operativi quando la logica di stato lo richiede).
- SOLID applicato in modo pratico.
- Clean Architecture con separazione netta dei layer.

Non e consentito introdurre endpoint o logica business che bypassino questi principi.

## 3) Layering obbligatorio per microservizio
Struttura:
- `*.Api`: endpoint Minimal API, mapping request/response, wiring.
- `*.Application`: comandi/query CQRS, handler, orchestrazione, contratti.
- `*.Domain`: aggregate, invarianti, value object, eventi dominio.
- `*.Infrastructure`: Marten, Wolverine, adapter esterni, persistenza, implementazioni.

Regole hard:
- `Domain` non dipende da framework IO.
- `Application` non dipende da implementazioni concrete.
- `Api` non contiene business logic.
- Marten/Wolverine solo in `Infrastructure`.

## 4) CQRS puro (obbligatorio)
Per ogni capability applicativa:
- Scritture: `Command` + `ICommandHandler` + dispatcher.
- Letture: `Query` + `IQueryHandler` + dispatcher.
- Endpoint sottili: delegano a command/query dispatcher.
- Evitare accesso diretto ai service di dominio negli endpoint quando la capability e parte del flusso CQRS.

Pattern vietati:
- Fat endpoint.
- Handler god-object.
- Logica business duplicata tra endpoint e handler.

## 5) Event-driven e integrazione
- Comunicazione backend tra microservizi via eventi/command async su RabbitMQ (Wolverine).
- Niente HTTP service-to-service nel checkout/workflow core.
- Stati terminali protetti (`Completed`/`Failed` non devono regredire se non con esplicita regola dominio approvata).
- Contratti eventi versionati `*V1`.
- Handler idempotenti e tolleranti a duplicati.

## 6) Minimal API conventions
- Route versionate `/v1/...`.
- `TypedResults` e `ProblemDetails` RFC 7807.
- Endpoint con naming/tag coerenti (`WithName`, `WithTags`).
- Validazione input ai boundary API.

Pattern obbligatori nei progetti `*.Api`:
- Contratti separati in `Contracts/Requests` e `Contracts/Responses` (namespace del modulo API).
- Gli endpoint non costruiscono payload/DTO complessi inline.
- Mapping centralizzato in classi statiche `*Mapper.cs` (es. `OrderMapper`, `PaymentMapper`).
- Il mapper gestisce:
  - `Request -> Command/Integration Payload`
  - `Application View/Model -> Response`
- Niente business rules nei mapper: solo conversione strutturale e default tecnici.
- Convenzioni naming:
  - `To<CommandName>`, `To<IntegrationPayloadName>`, `ToResponse`, `To<SpecificResponseName>`.

## 7) SOLID e Clean Code
- SRP: una classe, una responsabilita primaria.
- DIP: dipendenze su astrazioni.
- ISP: interfacce piccole e focalizzate.
- OCP: estensione tramite composizione/strategie, non switch monolitici.
- Nomi espliciti e ricercabili; no abbreviazioni ambigue.

## 8) Frontend/backoffice
- Backoffice separato dal web pubblico.
- UX operativa chiara: stato vuoto leggibile, azioni esplicite, flussi amministrativi completi.
- Per nuove sezioni operative (es. spedizioni), creare pagina dedicata e integrazione nel menu.
- Coerenza visuale con design system esistente (Tailwind + classi condivise).

## 9) Testing e DoD
Ogni modifica rilevante deve includere o aggiornare test pertinenti:
- Unit test per handler/validator/invarianti.
- Integration test per endpoint e flussi critici publish/consume.

Definition of Done minima:
- Build verde.
- Nessun errore analizzatori rilevante.
- Contratti API/eventi coerenti.
- Documentazione aggiornata se cambia comportamento pubblico o architettura.

## 10) Sicurezza e operativita
- Nessun segreto in codice o log.
- Configurazione via env.
- Logging strutturato con correlation context.
- Mantenere bootstrap docker-first funzionante (`docker compose up --build`).

## 11) Regole operative per Copilot
Quando implementi:
- Fai modifiche minime ma complete end-to-end.
- Non introdurre nuove dipendenze senza necessità forte.
- Non rompere confini tra bounded context.
- Se una richiesta viola queste linee, proporre alternativa compatibile prima di procedere.
