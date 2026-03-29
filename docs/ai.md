# AGENTS.md

> Purpose: define standards and principles for producing readable, testable, maintainable, and secure code.
> Applies to humans and AI agents working in this repository.

---

## 1) North Star: quality over speed

### Non-negotiable goals
- **Correctness**: code must work and handle edge cases.
- **Readability**: code is read more often than written.
- **Maintainability**: easy changes, hard regressions.
- **Testability**: design that supports automated testing.
- **Security**: no secrets in the repo; validate all inputs.
- **Consistency**: uniform conventions across the project.

### Golden rule
> If it’s not clear *what* it does and *why* it does it, it’s not done.

---

## 2) Clean Code principles

### Naming
- Names must be **explicit**, **pronounceable**, **searchable**.
- Avoid ambiguous abbreviations.
- Functions: verb + object (`calculateTotal`, `fetchUserById`).
- Classes/modules: nouns (`OrderService`, `InvoiceRepository`).
- Booleans: `is/has/can/should` prefixes.

### Small functions, single purpose
- A function should do **one thing**.
- Parameters: keep them few and clear; use objects/structs when needed.
- Avoid boolean flags that change behavior (code smell).

### Comments
- Prefer **self-explanatory code** over comments.
- Comment the **why**, not the **what** (the code should show the what).
- Keep comments accurate or delete them.

### Structure & complexity
- Reduce nesting (early returns, guard clauses).
- Avoid duplication (DRY) but don’t “abstract too early”.
- Watch cyclomatic complexity; refactor when it grows.

---

## 3) SOLID (practical guidance)

### S — Single Responsibility
- One module/class changes for **one** reason.
- If it has multiple reasons to change → split it.

### O — Open/Closed
- Extend behavior without modifying existing code:
  - strategies, polymorphism, composition, well-designed feature flags.

### L — Liskov Substitution
- Subtypes must honor contracts:
  - no surprises in pre/post conditions or exceptions.

### I — Interface Segregation
- Small interfaces tailored to clients.
- Avoid “god interfaces” with unused methods.

### D — Dependency Inversion
- Depend on abstractions, not details:
  - dependency injection, ports & adapters, inverted dependencies.

---

## 4) Architecture: separation of concerns

### Suggested layers (adapt to the project)
- **Domain/Core**: business rules, invariants, value objects.
- **Application/Use Cases**: orchestration, transactions, workflows.
- **Infrastructure**: DB, HTTP, filesystem, brokers, external services.
- **Interface/API/UI**: controllers/handlers, input/output mapping.

### Practical rules
- The **domain** must not depend on frameworks or IO.
- IO and details live at the edges.
- Keep boundaries explicit (folders/modules/packages).

---

## 5) Design Patterns: when and how to use them

> Patterns are shared vocabulary. Don’t use them “just to use patterns”.

### Common patterns (typical choices)
- **Factory / Abstract Factory**: complex creation or variants.
- **Strategy**: interchangeable algorithms (avoid growing `if/switch` trees).
- **Adapter**: integrate external APIs without polluting the domain.
- **Facade**: simplify complex subsystems.
- **Decorator**: add behavior without rigid inheritance.
- **Observer / Pub-Sub**: events and decoupling (mind debugging complexity).
- **Command**: actions with retry/audit/queueing needs.
- **Repository**: abstract data access (don’t hide business logic in it).
- **Unit of Work / Transaction Script**: transactional consistency where needed.
- **State**: explicit state machines for complex workflows.
- **Template Method**: controlled variation over a common flow.

### Anti-patterns to avoid
- God Object / God Service
- Singleton as global state (prefer DI)
- Over-engineering and “pattern-first” design
- Refactors that add abstraction without reducing complexity

---

## 6) Error handling, contracts, and invariants

- Validate inputs at boundaries (API, file, queue).
- Use **typed errors/exceptions** or consistent error codes.
- Helpful error messages without leaking sensitive info.
- Domain invariants:
  - prefer constructors/factories that guarantee valid objects.
- Avoid silent catch-alls: handle intentionally and log appropriately.

---

## 7) Logging, observability, and diagnostics

- Prefer structured logs when possible (keys like request_id, user_id, correlation_id).
- Consistent levels: debug/info/warn/error.
- Never log secrets, tokens, passwords, or unnecessary personal data.
- Add metrics/events for critical paths (latency, error rate, retries).

---

## 8) Testing: pyramid, coverage, reliability

### Priorities
- **Unit tests**: fast, deterministic, isolated.
- **Integration tests**: real DB/HTTP or containerized dependencies.
- **E2E tests**: few and focused on critical flows.

### Rules
- Every bug fix adds a test that fails before and passes after.
- Tests must be independent (no implicit ordering).
- Avoid flakiness: control time, randomness, network.
- Mock only what is external/unstable; prefer simple fakes/stubs.

### Minimum Definition of Done
- New features: tests + essential docs.
- Refactors: existing tests stay green; add tests if behavior changes.

---

## 9) Security & secrets management

- No credentials in code or logs.
- Use environment variables / secret managers.
- Keep dependencies updated; address vulnerabilities.
- Sanitize inputs and prevent injection (SQL, command, template).
- Apply least privilege for roles/keys.

---

## 10) Performance & scalability (no premature optimization)

- Measure first, then optimize (profiling).
- Avoid N+1, unnecessary queries, excessive allocations in hot loops.
- Cache only with a clear, testable invalidation strategy.
- Timeouts and backoff retries for external calls.

---

## 11) Dependencies, tooling, and automated quality gates

- Formatter + linter required (CI must fail if non-compliant).
- Static analysis where available.
- Pre-commit hooks recommended.
- Regular dependency updates (with changelog review and tests).

---

## 12) Git, PRs, and code review

### Commits
- Small, descriptive commits (Conventional Commits preferred).
- One commit = one coherent change.

### Pull Requests
- Describe: problem, approach, trade-offs, how to test.
- Include screenshots/logs when helpful.
- Avoid mega-PRs: split into smaller steps.

### Review checklist
- Readability and naming
- Complexity and duplication
- Adequate tests
- Error handling and edge cases
- Security and privacy
- Compatibility and migrations (DB/schema)
- Documentation updates

---

## 13) Guidelines for AI agents modifying this repo

### Working mode
- Prefer **minimal, targeted changes**.
- Match existing style and conventions.
- Don’t add new libraries without strong justification.
- Avoid “cosmetic refactors” unless requested.

### Before changing code
- Identify the correct extension point (SOLID boundaries).
- Find related tests; add missing ones.
- Respect architectural boundaries (domain vs infrastructure).

### While implementing
- Leave the code better than you found it.
- Update/add tests.
- Update docs if public behavior changes.

---

## 16) Decisions and ADRs
- Document significant architectural decisions in `docs/adr/`.
- Start from `docs/adr/_template.md` and update `docs/adr/README.md` index.
- Each ADR includes: context, decision, alternatives, consequences, trade-offs.
- When a decision changes, create a new ADR and mark the old one as superseded.
- Keep architecture docs aligned with accepted ADRs:
  - `docs/architecture.md`
  - `docs/bounded-contexts/`
  - `docs/guidelines/`

---

## 17) Final quality bar
> Code is ready when:
- Build passes (no meaningful warnings) 
- Relevant tests are green
- Lint/format checks pass
- Edge cases are handled
- No secrets are exposed
- README/changelog updated if needed
