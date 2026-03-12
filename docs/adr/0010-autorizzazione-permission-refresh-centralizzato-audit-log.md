# ADR-0010: Autorizzazione admin per permission, refresh centralizzato frontend e audit auth strutturato

- Data: 2026-03-12
- Stato: Accepted
- Decisori: Team backend/frontend e-commerce
- Consultati: Stakeholder prodotto e operation
- Informati: Team progetto

## Contesto

L'assetto auth precedente presentava questi limiti principali:
- endpoint admin protetti principalmente da `realm=admin`, senza enforcement per permission endpoint-by-endpoint;
- backoffice con gestione token lato browser e assenza di refresh automatico centralizzato su `401`;
- endpoint forgot/reset/resend verification con `codePreview` restituito anche fuori development;
- assenza di un audit trail esplicito per eventi auth critici (login/refresh/logout/reset password).

Il progetto deve mantenere separazione dei bounded context, minimizzare regressioni operative e introdurre miglioramenti incrementali senza forzare subito una migrazione completa a BFF.

## Decisione

Si adotta il seguente pacchetto di decisioni:

1. Applicare policy admin granulari basate su claim `permission` su tutti gli endpoint admin dei contesti Account/Catalog/Order/Shipping/Warehouse.
2. Mantenere in questa iterazione l'architettura backoffice corrente (no BFF completo), introducendo pero refresh automatico centralizzato lato frontend admin con retry singolo su `401`.
3. Rendere `codePreview` disponibile solo in ambiente development.
4. Introdurre audit trail auth tramite log strutturati (senza nuova tabella DB in questa fase).

## Alternative considerate

1. BFF completo immediato con access token `httpOnly` e forward server-side:
   pro sicurezza migliore lato browser; contro impatto alto e refactor ampio in singola iterazione.
2. Nessuna policy permission-based e mantenimento `AdminPolicy` unica:
   pro semplicità; contro autorizzazione troppo coarse-grained.
3. Audit trail su tabella dedicata Account:
   pro query storiche strutturate; contro migration/schema e maggiore complessita operativa immediata.

## Conseguenze

### Positive

- Migliore principio least-privilege sugli endpoint admin.
- Miglior resilienza UX del backoffice su scadenza access token (`401` -> refresh -> retry singolo).
- Riduzione esposizione di informazioni sensibili (`codePreview`) fuori dev.
- Tracciabilita operativa degli eventi auth critici con outcome success/failure.

### Negative / Trade-off

- Il backoffice continua a non essere un BFF completo; resta debito tecnico su token handling browser-side.
- Audit trail a log richiede disciplina di retention/monitoring e non offre query relazionali native come una tabella dedicata.
- La gestione permission richiede mantenimento allineato tra emissione claim e mapping policy endpoint.

## Impatto su implementazione

- Componenti coinvolti:
  - `Shared.BuildingBlocks.Api` (policy/permission shared),
  - `Account.Infrastructure` (policy module, auth service),
  - endpoint admin in `Account.Api`, `Catalog.Api`, `Order.Api`, `Shipping.Api`, `Warehouse.Api`,
  - `frontend/admin` (`api.ts`, `auth.ts`, endpoint `api/auth/refresh`).
- Impatto su deployment e operation:
  - nessuna migration DB aggiuntiva;
  - necessario monitorare i nuovi log auth e i failure rate refresh.
- Impatto su test e qualita:
  - aggiunti test su permission token/policy registration, su `codePreview` env-based e su audit warning login invalid.

## Piano di adozione

1. Introdurre costanti shared claim/permission/policy e aggiornare registrazione authorization.
2. Migrare endpoint admin a policy granulari read/write per dominio.
3. Aggiornare `AccountAuthService` per permission admin estese, `codePreview` env-based e audit log strutturati.
4. Introdurre refresh centralizzato frontend admin con retry singolo su `401` e endpoint server-side di refresh.
5. Aggiornare documentazione auth e test di regressione.

## Riferimenti

- `docs/authentication.md`
- `src/Shared.BuildingBlocks/Api/AuthorizationPolicies.cs`
- `src/Shared.BuildingBlocks/Api/AuthenticationExtensions.cs`
- `src/Account.Infrastructure/Services/AccountAuthService.cs`
- `frontend/admin/src/lib/api.ts`
- `frontend/admin/src/pages/api/auth/refresh.ts`
- ADR correlate: `0008-strategia-test-backend.md`, `0009-servizio-comunicazioni-email-event-driven.md`
