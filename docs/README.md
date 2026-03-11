# Documentazione Architetturale

Questa cartella contiene decisioni architetturali, linee guida e documenti di riferimento per il backend e-commerce.

## Indice rapido

- `agent-guidelines.md`: standard generali di sviluppo.
- `architecture.md`: vista architetturale target e confini tra bounded context.
- `authentication.md`: implementazione autenticazione/autorizzazione (store + backoffice).
- `adr/README.md`: indice Architecture Decision Records.
- `bounded-contexts/`: responsabilita e confini per ciascun contesto.
- `guidelines/`: linee guida operative per implementazione incrementale.
- `guidelines/catalog-baseline-conventions.md`: convenzioni tattiche standard derivate dal refactor Catalog.
- `guidelines/endpoint-conventions.md`: convenzioni endpoint HTTP (path, contesti, versioning, whitelist).

## Principi guida

1. Dominio prima della tecnologia.
2. Microservizi pragmatici: isolamento semantico, complessita operativa controllata.
3. Contract-first: API ed eventi versionati e backward-compatible.
4. Eventual consistency esplicita con compensazioni e idempotenza.
5. Evoluzione incrementale per vertical slice, evitando big-bang.

## Come usare questa documentazione

1. Leggere `architecture.md` per il quadro generale.
2. Consultare gli ADR in `adr/` prima di introdurre nuove decisioni tecniche.
3. Seguire le linee guida in `guidelines/` durante l'implementazione.
4. Aggiornare un ADR quando una decisione architetturale cambia.
