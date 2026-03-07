# ADR-0004: Contract-first e versioning di API ed eventi

- Data: 2026-03-07
- Stato: Accepted
- Decisori: Product/Tech Owner
- Consultati: Stakeholder progetto
- Informati: Team backend/frontend

## Contesto

Con bounded context separati, la stabilita dei contratti e fondamentale per evitare regressioni tra servizi e frontend. L'evoluzione del dominio richiede cambi frequenti, ma le integrazioni devono restare compatibili.

## Decisione

Adottare una governance contract-first per API ed eventi:

1. ogni endpoint/evento pubblico e definito come contratto versionato;
2. breaking change solo con nuova versione (`v2`, `V2`);
3. non-breaking change consentite solo se backward-compatible;
4. deprecazione con finestra temporale esplicita e comunicata.

## Alternative considerate

1. Code-first senza governance: piu veloce nel breve, ma alto rischio di rotture tra contesti.
2. Versionamento solo per API HTTP: insufficiente nei workflow event-driven.
3. Versionamento ad hoc per team: incoerenza e debito tecnico crescente.

## Conseguenze

### Positive

- Evoluzione prevedibile dei contratti.
- Maggiore stabilita tra producer e consumer.
- Migliore separazione tra modello interno e modello di integrazione.

### Negative / Trade-off

- Maggior overhead di documentazione e review.
- Necessita di test di compatibilita contrattuale.

## Impatto su implementazione

- Definire DTO/API contract indipendenti dal dominio interno.
- Introdurre regole di naming/versioning uniformi.
- Aggiornare documentazione e changelog ad ogni versione.

## Piano di adozione

1. Catalogare contratti API/eventi attuali per contesto.
2. Definire policy di compatibilita e deprecazione.
3. Aggiungere contract tests nel pipeline di build.

## Riferimenti

- `./0002-comunicazione-inter-context.md`
- `./0003-data-ownership-database-separati.md`
- `../guidelines/integration-events.md`
