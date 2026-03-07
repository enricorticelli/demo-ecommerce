# Roadmap Implementazione Backend (Da Mock a Reale)

## Principio

Eseguire evoluzione incrementale per vertical slice, mantenendo il sistema sempre deployabile.

## Fase 1: Fondazioni

1. Allineare namespace e tipi mancanti per rendere compilabili i test esistenti.
2. Introdurre primi command/handler e astrazioni service nei contesti core.
3. Eliminare risposte stub sui percorsi critici.

## Fase 2: Core flow ordine

1. `Order`: creazione ordine con invarianti dominio.
2. `Payment`: autorizzazione pagamento reale.
3. `Shipping`: creazione spedizione guidata da evento.
4. `Warehouse`: riserva stock e risposta al workflow.

## Fase 3: Hardening

1. Idempotenza consumer e deduplica.
2. Retry policy e gestione errori transient.
3. Osservabilita: correlation id, metriche principali, tracing.

## Fase 4: Estensione funzionale

1. Catalog CRUD completo non mock.
2. Cart persistente con checkout robusto.
3. Miglioramento query/read model per frontend.

## Criteri di uscita da fase

1. Build green.
2. Test unitari e integrazione verdi per i flussi introdotti.
3. Contratti API/eventi documentati e compatibili.
4. Nessun endpoint critico che ritorna dati fittizi.

## ADR correlate

- `../adr/0001-microservices-pragmatici.md`
- `../adr/0002-comunicazione-inter-context.md`
- `../adr/0003-data-ownership-database-separati.md`
- `../adr/0004-contract-first-versioning.md`
- `../adr/0005-eventual-consistency-compensazioni.md`
- `../adr/0006-idempotenza-deduplica.md`
- `../adr/0007-osservabilita-distribuita-minima.md`
- `../adr/0008-strategia-test-backend.md`
