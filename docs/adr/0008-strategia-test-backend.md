# ADR-0008: Strategia test backend per architettura distribuita

- Data: 2026-03-07
- Stato: Accepted
- Decisori: Product/Tech Owner
- Consultati: Stakeholder progetto
- Informati: Team backend/frontend

## Contesto

La transizione da endpoint mock a backend reale richiede sicurezza evolutiva. In una architettura distribuita servono test su dominio, integrazione locale e compatibilita contratti.

## Decisione

Adottare una strategia test multilivello:

1. unit test su dominio e application handler;
2. integration test per endpoint e persistenza per contesto;
3. contract test per API/eventi cross-context;
4. test end-to-end selettivi sui workflow core.

## Alternative considerate

1. Solo E2E: costosi, lenti e fragili per diagnosi.
2. Solo unit test: copertura insufficiente su integrazioni reali.
3. Test ad hoc senza strategia: crescita non governata della suite.

## Conseguenze

### Positive

- Riduzione regressioni su evoluzione dei servizi.
- Maggiore confidenza su refactoring e cambi contrattuali.
- Feedback rapido su errori di dominio e integrazione.

### Negative / Trade-off

- Tempo iniziale per impostare pipeline e fixture.
- Manutenzione continua dei test di contratto.

## Impatto su implementazione

- Definire standard test per ciascun contesto.
- Rendere compilabili e attendibili i test esistenti.
- Introdurre quality gates progressivi in CI.

## Piano di adozione

1. Rendere verdi i test dei contesti core.
2. Aggiungere contract tests su eventi e API condivise.
3. Attivare quality gate minimi in pipeline.

## Riferimenti

- `./0004-contract-first-versioning.md`
- `./0006-idempotenza-deduplica.md`
- `../guidelines/implementation-roadmap.md`
