# Architecture Decision Records (ADR)

## Stato

- `Accepted`: decisione attiva e applicabile.
- `Proposed`: in valutazione.
- `Superseded`: sostituita da una ADR piu recente.
- `Deprecated`: mantenuta solo per storico.

## Indice ADR

| ADR | Titolo | Stato | Data |
| --- | --- | --- | --- |
| [0001](./0001-microservices-pragmatici.md) | Modello architetturale: microservizi pragmatici | Accepted | 2026-03-07 |
| [0002](./0002-comunicazione-inter-context.md) | Comunicazione tra bounded context | Accepted | 2026-03-07 |
| [0003](./0003-data-ownership-database-separati.md) | Ownership dati e database separati | Accepted | 2026-03-07 |
| [0004](./0004-contract-first-versioning.md) | Contract-first e versioning contratti | Accepted | 2026-03-07 |
| [0005](./0005-eventual-consistency-compensazioni.md) | Eventual consistency e compensazioni | Accepted | 2026-03-07 |
| [0006](./0006-idempotenza-deduplica.md) | Idempotenza e deduplica messaggi | Accepted | 2026-03-07 |
| [0007](./0007-osservabilita-distribuita-minima.md) | Osservabilita distribuita minima | Accepted | 2026-03-07 |
| [0008](./0008-strategia-test-backend.md) | Strategia test backend | Accepted | 2026-03-07 |
| [0009](./0009-servizio-comunicazioni-email-event-driven.md) | Servizio Communication per email esterne event-driven | Accepted | 2026-03-08 |

## Regole di manutenzione

1. Nuova decisione non banale: creare nuova ADR da template.
2. Cambio di decisione: non modificare il passato, creare ADR nuova e marcare la vecchia come `Superseded`.
3. Ogni ADR deve riportare contesto, decisione, alternative, conseguenze e trade-off.
4. Ogni ADR accettata deve essere referenziata dalla documentazione interessata.
