# E-commerce

Backend e-commerce basato su microservizi pragmatici con Clean Architecture e event-driven integration.

---

## Descrizione

Il progetto implementa un sistema e-commerce backend composto da bounded context indipendenti, ciascuno con il proprio modello di dominio, database e API. La comunicazione sincrona avviene tramite un API Gateway HTTP; la comunicazione asincrona tra contesti usa eventi di integrazione su RabbitMQ.

Bounded context inclusi:

| Contesto    | Responsabilità                                      |
|-------------|-----------------------------------------------------|
| `Catalog`   | Gestione catalogo prodotti e metadati commerciali   |
| `Cart`      | Carrello e stato pre-ordine                         |
| `Order`     | Lifecycle ordine e orchestrazione del processo      |
| `Payment`   | Autorizzazione e stato pagamento                    |
| `Shipping`  | Creazione e avanzamento spedizioni                  |
| `Warehouse` | Disponibilità e riserva stock                       |
| `Communication` | Invio comunicazioni esterne (email)             |
| `Account`   | Identity e profilo cliente/backoffice               |
| `Gateway`   | Routing HTTP, nessuna logica di dominio             |

---

## Quick Start

### Prerequisiti

- [Docker](https://www.docker.com/) e Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/)
- [Node.js](https://nodejs.org/) (opzionale, per i frontend e lo script di seeding)

### Avvio

```bash
# 1. Copia le variabili d'ambiente
cp .env.example .env

# 2. Avvia l'infrastruttura e tutti i servizi
docker compose up -d

# 3. (Opzionale) Seed del catalogo
node scripts/seeding/seed-catalog.js
```

Lo script di seeding esegue login admin automatico con `Account__Admin__Username` e `Account__Admin__Password` definiti in `.env`.

I servizi saranno disponibili tramite il gateway su `http://localhost:18080`.
L'Aspire Dashboard per l'osservabilità è raggiungibile su `http://localhost:18890`.
Mailpit (mock SMTP + inbox UI) è disponibile su `http://localhost:18025`.
Payment mock gateway (hosted checkout esterno) è disponibile su `http://localhost:18082`.

### Hot reload microservizi .NET (Docker)

Per sviluppo locale e disponibile `docker-compose.override.yml`, che avvia le API .NET con `dotnet watch`.

```bash
# avvio con hot reload (compose carica automaticamente docker-compose.override.yml)
docker compose up -d

# stop completo
docker compose down
```

Note:
- Il primo avvio e piu lento per restore/build.
- Su Windows e abilitato `DOTNET_USE_POLLING_FILE_WATCHER=1` per rilevare correttamente le modifiche dei file montati.

### Contesti API

Il gateway espone solo endpoint contestualizzati:

- `store`: `/api/store/{service}/v1/...`
- `admin`: `/api/admin/{service}/v1/...`

Esempi:

- Storefront catalogo: `GET /api/store/catalog/v1/products`
- Backoffice catalogo: `POST /api/admin/catalog/v1/products`

---

## Architettura

```
┌─────────────┐     HTTP      ┌───────────────┐
│  Frontend   │ ──────────── ▶│  Gateway.Api  │
└─────────────┘               └──────┬────────┘
                                     │ HTTP
              ┌──────────────────────┼──────────────────────┐
              ▼                      ▼                       ▼
        Catalog.Api           Cart.Api / Order.Api     Payment.Api
        Warehouse.Api                                  Shipping.Api
              │                      │                       │
              └──────────────────────┴───────────────────────┘
                                     │ Events
                                ┌────▼────┐
                                │RabbitMQ │
                                └─────────┘

Ogni servizio ha il proprio database PostgreSQL isolato.
```

Ogni microservizio rispetta la struttura:

```
<Context>.Api            — Endpoint, Contracts, Mappers
<Context>.Application    — Commands, Queries, Handlers, Services
<Context>.Domain         — Entità, Value Object, Domain Events
<Context>.Infrastructure — Repository, Outbox, Adapter tecnici
```

---

## Documentazione

La documentazione estesa si trova nella cartella [`docs/`](docs/):

| Documento | Contenuto |
|-----------|-----------|
| [docs/architecture.md](docs/architecture.md) | Vista architetturale target e vincoli |
| [docs/agent-guidelines.md](docs/agent-guidelines.md) | Linee guida operative di sviluppo |
| [docs/adr/](docs/adr/) | Architecture Decision Records |
| [docs/bounded-contexts/](docs/bounded-contexts/) | Responsabilità e confini di ciascun contesto |
| [docs/guidelines/](docs/guidelines/) | Linee guida implementative per vertical slice |
