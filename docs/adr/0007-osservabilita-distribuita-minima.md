# ADR-0007: Osservabilita distribuita minima obbligatoria

- Data: 2026-03-07
- Stato: Accepted
- Decisori: Product/Tech Owner
- Consultati: Stakeholder progetto
- Informati: Team backend/frontend

## Contesto

In una architettura distribuita, il debugging senza osservabilita e costoso e lento. Per un team singolo serve una baseline minima ma solida, senza piattaforme troppo complesse in prima fase.

## Decisione

Adottare uno standard minimo obbligatorio di osservabilita:

1. logging strutturato con `correlationId` in tutti i servizi;
2. metriche base su throughput, errori, latenza endpoint e handler;
3. tracing distribuito per i workflow principali;
4. health checks `live` e `ready` in ogni servizio.

## Alternative considerate

1. Solo log testuali: insufficiente per tracing cross-context.
2. Observability completa enterprise da subito: overhead non proporzionato.
3. Nessuno standard comune: incident management imprevedibile.

## Conseguenze

### Positive

- Riduzione MTTR su incidenti applicativi.
- Maggiore visibilita sui colli di bottiglia.
- Migliore affidabilita operativa nel tempo.

### Negative / Trade-off

- Costo iniziale di strumentazione e dashboard.
- Necessita di governance sui dati di log/metriche.

## Impatto su implementazione

- Definire middleware comuni per correlation id.
- Standardizzare naming di metriche e dimensioni.
- Predisporre dashboard minime per flusso ordine.

## Piano di adozione

1. Introdurre logging strutturato in tutti i context.
2. Aggiungere metriche e tracing nel flow ordine.
3. Estendere progressivamente agli altri workflow.

## Riferimenti

- `./0002-comunicazione-inter-context.md`
- `./0005-eventual-consistency-compensazioni.md`
- `../architecture.md`
