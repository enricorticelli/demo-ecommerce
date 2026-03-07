# ADR-0005: Eventual consistency e compensazioni nei workflow distribuiti

- Data: 2026-03-07
- Stato: Accepted
- Decisori: Product/Tech Owner
- Consultati: Stakeholder progetto
- Informati: Team backend/frontend

## Contesto

I flussi ordine coinvolgono piu bounded context con ownership dati separata. La coerenza forte cross-context non e realistica senza aumentare drasticamente coupling e costo operativo.

## Decisione

Adottare eventual consistency tra bounded context con regole esplicite:

1. transazione forte solo dentro il singolo contesto;
2. workflow cross-context guidati da eventi;
3. gestione errori con compensazioni applicative;
4. stati intermedi espliciti nei processi lunghi.

## Alternative considerate

1. Coerenza forte distribuita: elevato costo tecnico/operativo e ridotta resilienza.
2. Best-effort senza compensazioni: rischio alto di stati incoerenti permanenti.
3. Orchestrazione centralizzata hard-coded: fragile e poco evolutiva.

## Conseguenze

### Positive

- Migliore resilienza su failure parziali.
- Scalabilita e autonomia dei contesti.
- Maggiore chiarezza dei processi asincroni.

### Negative / Trade-off

- Esistenza di inconsistenze temporanee.
- Necessita di meccanismi di riconciliazione e monitoraggio.

## Impatto su implementazione

- Modellare stati ordine/pagamento/spedizione con transizioni esplicite.
- Definire azioni compensative per ogni step critico.
- Esporre stati di processo in modo trasparente alle API consumer.

## Piano di adozione

1. Definire mappa stati e transizioni per i workflow core.
2. Implementare compensazioni minime per i failure path.
3. Aggiungere metriche e alert su workflow bloccati.

## Riferimenti

- `./0002-comunicazione-inter-context.md`
- `./0003-data-ownership-database-separati.md`
- `../architecture.md`
