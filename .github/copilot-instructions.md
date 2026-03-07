# Copilot Instructions - CQRS E-commerce

Le istruzioni operative condivise sono centralizzate in:
- `docs/agent-guidelines.md`

## Precedenza documentale
In caso di conflitto, prevalgono:
1. `docs/technical-guidelines.md`
2. `docs/architecture.md`
3. `docs/agent-guidelines.md`
4. questo file

## Lettura obbligatoria
Prima di modificare codice, leggere i documenti in `docs/`, almeno:
- `docs/technical-guidelines.md`
- `docs/architecture.md`
- `docs/api-patterns.md`
- `docs/checkout-flow.md` (se tocchi checkout/workflow)
- `docs/adr/*.md` pertinenti

Applicare modifiche minime ma complete, senza violare CQRS/Clean Architecture/SOLID e senza bypass dei confini tra bounded context.
