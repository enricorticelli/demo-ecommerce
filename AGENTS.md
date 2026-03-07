# AGENTS.md - CQRS E-commerce

Le regole operative per agent automatici sono centralizzate in:
- `docs/agent-guidelines.md`

## Ordine di precedenza
In caso di conflitto tra documenti, prevale questo ordine:
1. `docs/technical-guidelines.md`
2. `docs/architecture.md`
3. `docs/agent-guidelines.md`
4. `.github/copilot-instructions.md`

## Lettura obbligatoria
Prima di qualsiasi modifica codice, leggere i documenti in `docs/`, in particolare:
- `docs/technical-guidelines.md`
- `docs/architecture.md`
- `docs/api-patterns.md`
- `docs/checkout-flow.md` (se pertinente)
- `docs/adr/*.md` pertinenti

Se una richiesta viola le linee guida, proporre prima un'alternativa compatibile.
