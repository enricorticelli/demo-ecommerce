# Linee Tecniche e Pattern

Questo documento definisce le regole tecniche vincolanti del repository.
Obiettivo: mantenere coerenza architetturale, performance e manutenibilita' su tutti i microservizi.

## 1. Obiettivi architetturali (non negoziabili)
- Correttezza funzionale e robustezza sugli edge case.
- Separazione netta delle responsabilita' (SOLID).
- Performance e affidabilita' prioritarie rispetto alla feature depth.
- Stack OSS e commercial-use friendly.
- `docker compose up --build` deve portare il sistema avviabile end-to-end.

## 2. Stack e vincoli
- Backend: .NET 10 Minimal API.
- Event sourcing/document DB: Marten su PostgreSQL.
- Messaging async: Wolverine + RabbitMQ.
- Gateway: YARP, volutamente semplice (no clean architecture dedicata).
- Frontend: Astro SSR + Svelte islands + Nanostores + Tailwind.
- Auth/Identity: non prevista in questa fase.

## 3. Service boundaries
Servizi previsti:
- Catalog
- Warehouse
- Shipping
- Order
- Cart
- User
- Payment
- Gateway

Regole:
- Data-per-service logica (schema dedicato per servizio).
- Niente DB sharing diretto tra bounded context.
- Comunicazione HTTP sincrona solo tra client esterni (frontend/backoffice) e gateway/API pubbliche.
- Niente chiamate HTTP dirette service-to-service nei workflow core (checkout/order fulfillment).
- Comunicazione tra microservizi backend via eventi/command async su broker.

## 4. Layering e dipendenze
Per microservizio backend:
- `*.Api`: entrypoint, endpoint, wiring DI.
- `*.Application`: use case, contratti, validatori, orchestration astratta.
- `*.Domain`: invarianti, aggregate, value object/eventi dominio.
- `*.Infrastructure`: implementazioni tecniche (Marten/Wolverine/repository/adapter esterni).

Vincoli hard:
- `Domain` non deve dipendere da framework IO.
- `Application` non deve conoscere implementazioni concrete.
- `Api` e `Application` non devono usare direttamente Marten/Wolverine.
- Marten/Wolverine solo in `Infrastructure`.

Eccezione esplicita:
- `Gateway.Api` resta semplice e non segue split a 4 layer.

## 5. SOLID applicato
- SRP: una classe = una responsabilita' primaria.
- OCP: estensioni via interfacce/strategie, evitare switch monolitici.
- LSP: implementazioni conformi ai contratti applicativi.
- ISP: interfacce piccole e focalizzate.
- DIP: dipendenze tra layer solo tramite astrazioni.

Pattern di implementazione usati:
- Workflow handlers separati per evento (niente god-handler).
- Orchestrator/process manager separato dai transport adapters.
- Transport asincrono broker-first tra microservizi.
- Event publishing dietro astrazione (`IOrderEventPublisher`).
- State store dietro astrazione (`IOrderStateStore`).

## 6. CQRS + Event Sourcing
- Write model e read model separati.
- Event sourcing usato almeno su `Cart` e `Order`.
- CQRS puro in tutti i moduli backend: ogni capability pubblica deve passare da `Command/Query` + handler + dispatcher.
- Aggregate con transizioni di stato esplicite e invarianti difensive.
- Read endpoint ottimizzati per query.

## 7. Messaging e affidabilita'
Eventi integrazione (v1):
- `cart.checked_out.v1`
- `order.placed.v1`
- `warehouse.stock_reserved.v1`
- `warehouse.stock_rejected.v1`
- `payment.authorized.v1`
- `payment.failed.v1`
- `shipping.created.v1`
- `order.completed.v1`
- `order.failed.v1`

Regole:
- Wolverine durable messaging con inbox/outbox.
- Handler idempotenti e tolleranti a duplicati dove possibile.
- Retry policy/timeouts su integrazioni esterne fuori dal perimetro microservizi (es. PSP, servizi terzi).
- Stati terminali protetti (`Completed`/`Failed` non devono regredire).
- Eccezione approvata: da backoffice e consentito `Completed -> Failed` solo tramite azione manuale esplicita e tracciabile dell'operatore.

Policy checkout/order:
- `Order` non deve chiamare via HTTP `Cart`, `Warehouse`, `Payment`, `Shipping`.
- Lo stato ordine avanza solo da eventi integrazione.
- Le API order accettano payload sufficiente a iniziare il workflow senza fetch sincroni cross-service.

## 8. API design
- Versioning uniforme: `/v1/...`.
- Errori HTTP in RFC 7807 Problem Details.
- Endpoint Minimal API con `TypedResults`.
- Endpoint naming coerente (`WithName`, `WithTags`).
- FluentValidation su input command.
- Niente endpoint interni di seed hardcoded: il seeding passa da API pubbliche CRUD.
- Per moduli operativi (es. `Shipping`) esporre capability gestionali tramite endpoint CQRS dedicati (lista, dettaglio, aggiornamento stato) con endpoint sottili.

Pattern standard nei progetti `*.Api`:
- Contratti API separati in:
  - `Contracts/Requests`
  - `Contracts/Responses`
- Endpoint con mapping minimale: delegano conversioni strutturate a mapper statici.
- Mapper in classi statiche `*Mapper.cs` vicine agli endpoint (tipicamente `Endpoints/`), con metodi per:
  - `Request -> Command/Integration Payload`
  - `Application View/Model -> Response`
- Vietato il mapping inline ripetitivo/complesso negli endpoint.
- Vietata logica di dominio nei mapper (solo conversione dati e fallback tecnici).
- Convenzioni naming raccomandate:
  - `To<CommandName>()`
  - `To<PayloadName>()`
  - `ToResponse()` o `To<SpecificResponseName>()`

Catalog (baseline):
- `GET /v1/products`
- `GET /v1/products/{id}`
- `POST /v1/products`
- `PUT /v1/products/{id}`
- `DELETE /v1/products/{id}`

## 9. Docker-first execution
Compose deve orchestrare:
- rabbitmq
- postgres
- gateway-api
- catalog-api
- warehouse-api
- shipping-api
- order-api
- cart-api
- user-api
- payment-api
- frontend-web
- frontend-admin

Regole operative:
- healthcheck su servizi API.
- dipendenze con `condition: service_healthy` quando necessario.
- volumi persistenti per RabbitMQ/PostgreSQL.
- configurazione via env (`.env.example`), no secret hardcoded.

## 10. Seeding e smoke/load
- Seeding tramite script esterno (`scripts/seed-and-smoke.mjs`).
- Lo script deve popolare prodotti via CRUD Catalog con dati random.
- Lo script deve preparare anche stock sufficiente per checkout reali.

## 11. Testing strategy
Minimo richiesto:
- Unit: validatori, invarianti aggregate, transizioni stato.
- Integration: endpoint principali e flusso publish/consume eventi critici.
- E2E smoke: checkout end-to-end con verifica ordine completato/fallito.

Tecnologie test:
- xUnit
- Moq

Posizionamento test:
- test collocati nelle solution folder dei microservizi (in `src/*Tests`).

## 12. Logging, tracing, osservabilita'
- Logging strutturato con CorrelationId.
- Endpoint health: `/health/live` e `/health/ready`.
- Evitare log di segreti o PII sensibili.
- Evoluzione consigliata: OpenTelemetry traces/metrics.

## 13. Performance playbook
- Async end-to-end, evitare blocchi sincroni.
- Event-driven checkout per ridurre coupling temporale.
- Read model ottimizzati per query frequenti.
- Evitare round-trip inutili e logica duplicata tra handler/endpoint.
- Misurare prima di ottimizzare (script load + metriche).

## 14. Anti-pattern da evitare
- Fat endpoint con business logic.
- God handler che processa tutti gli eventi eterogenei.
- Dipendenze concrete in `Api/Application/Domain`.
- Seeding via endpoint nascosti/non versionati.
- Condivisione database tra microservizi.

## 15. Definition of Done
Una modifica e' accettabile se:
- rispetta layering e SOLID.
- mantiene contratti API/eventi coerenti.
- aggiorna documentazione tecnica se cambia comportamento pubblico.
- aggiunge/aggiorna ADR in `docs/adr/` quando cambia una decisione architetturale.
- mantiene il bootstrap docker-first funzionante.
- include o aggiorna test pertinenti.
