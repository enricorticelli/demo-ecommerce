# Autenticazione e Autorizzazione: implementazione attuale

## Obiettivo

Questo documento descrive come e stata implementata l'autenticazione/autorizzazione nel progetto e-commerce, sia lato Storefront sia lato Backoffice.

Copre:
- architettura e componenti coinvolti
- modello token (JWT + refresh)
- endpoint e flussi
- integrazione frontend
- configurazione tecnica
- limiti noti e raccomandazioni

## Panoramica architetturale

L'autenticazione e centralizzata nel microservizio `Account.Api` e consumata tramite `Gateway.Api`.

Componenti principali:
- `Account.Api`: espone endpoint login/register/refresh/logout e profilo.
- `Account.Infrastructure`: implementa issuance token, validazione JWT, persistenza sessioni refresh.
- `Gateway.Api`: instrada le route `/api/store/account/...` e `/api/admin/account/...` verso `account-api`.
- Frontend Store (`frontend/web`): usa token in `localStorage`.
- Frontend Backoffice (`frontend/admin`): usa cookie (`bo_access_token`, `bo_refresh_token`) e middleware Astro.

## Modello di identità

### Realm

Il sistema usa due realm separati:
- `customer`
- `admin`

Il realm è salvato:
- nel claim JWT `realm`
- nella tabella `users`
- nella tabella `refresh_sessions`

### Policy autorizzative API

Policy configurate in `Account.Infrastructure`:
- `CustomerPolicy`: richiede claim `realm=customer`
- `AdminPolicy`: richiede claim `realm=admin`
- policy granulari admin basate su claim `permission`:
- `CatalogReadPolicy` / `CatalogWritePolicy`
- `OrdersReadPolicy` / `OrdersWritePolicy`
- `ShippingReadPolicy` / `ShippingWritePolicy`
- `WarehouseReadPolicy` / `WarehouseWritePolicy`
- `AccountAdminReadPolicy` / `AccountAdminWritePolicy`

Endpoint protetti:
- Store profile (`/store/v1/me`, `/me/addresses`, `/me/orders`) -> `CustomerPolicy`
- Admin endpoint-by-endpoint -> policy granulari `permission` (read/write per dominio)

## Token model

## Access token (JWT)

Generato da `TokenFactory.CreateAccessToken(...)` con firma HMAC SHA256.

Claim principali:
- `sub`: userId
- `email`: email utente
- `realm`: `customer` o `admin`
- `role`: `customer` o `admin`
- `email_verified`: `true/false`
- `permission`: uno o piu claim (uno per permission)

Issuer/Audience per realm:
- customer:
- issuer: `account-customer`
- audience: `storefront`
- admin:
- issuer: `account-admin`
- audience: `backoffice`

Durata default:
- access token: `30` minuti (`Account:Jwt:AccessTokenMinutes`)

Validazione JWT:
- issuer e audience validati
- firma validata con chiave simmetrica
- lifetime validata
- `ClockSkew` = 30 secondi

## Refresh token

Formato:
- token random base64 (48 byte random)

Durata default:
- `14` giorni (`Account:Jwt:RefreshTokenDays`)

Persistenza:
- tabella `refresh_sessions`
- viene salvato solo hash (`TokenHash`), non il token in chiaro

Rotazione on refresh:
- ad ogni `refresh`, la sessione corrente viene revocata (`RevokedAtUtc`)
- viene emessa una nuova coppia access+refresh
- viene creata una nuova `refresh_session`

Logout:
- revoca la refresh session associata al token ricevuto
- operazione idempotente (se token mancante o sessione assente, ritorna senza errore)

## Permission model attuale

Permissions hardcoded in `AccountAuthService`:

- customer:
- `account:read`
- `account:write`
- `orders:read`

- admin:
- `catalog:read`
- `catalog:write`
- `orders:read`
- `orders:write`
- `shipping:read`
- `shipping:write`
- `warehouse:read`
- `warehouse:write`
- `account:read`
- `account:write`

Nota:
- le policy admin usano `realm=admin` + claim `permission` richiesto dall'endpoint.
- l'endpoint `/me/permissions` espone il set effettivo di permission presenti nel token.
- per gli utenti admin e supportata assegnazione custom delle permission (per utente) da backoffice; se non configurate, viene usato il set admin di default.

## Formato risposta auth

Le operazioni login/register/refresh ritornano `AuthResponse`:
- `accessToken`
- `accessTokenExpiresAtUtc`
- `refreshToken`
- `refreshTokenExpiresAtUtc`
- `realm`
- `userId`
- `email`
- `permissions[]`

## Persistenza e schema DB (accounting)

Tabelle principali coinvolte:
- `users`
- `refresh_sessions`
- `email_verification_tokens`
- `password_reset_tokens`
- `customer_addresses`

Vincoli rilevanti:
- `users`: unique su `(Realm, NormalizedEmail)` e `(Realm, Username)`
- `refresh_sessions`: unique su `TokenHash`

## Flussi applicativi

## 1) Register customer

1. Storefront chiama `POST /users/register`.
2. Account crea utente `realm=customer`, hash password.
3. Crea anche codice verifica email (hash del codice).
4. Emette subito access+refresh token.

## 2) Login (store/admin)

1. Client invia username/email + password.
2. Account cerca utente nel realm richiesto.
3. Verifica password hash.
4. Emette access+refresh token e crea refresh session hashata.
5. Se customer verificato email, prova claim ordini guest via `OrderApiClient`.

## 3) Refresh

1. Client invia refresh token.
2. Account hash token e cerca sessione nel realm.
3. Se valida: revoca sessione corrente.
4. Emette nuova coppia token e nuova sessione.

## 4) Logout

1. Client invia refresh token.
2. Account revoca la sessione trovata (se esiste).

## 5) Verify email / reset password

- Codici numerici a 6 cifre, salvati come hash, con expiry a 20 minuti.
- Al reset password, tutte le sessioni refresh attive utente vengono revocate.

## Frontend Store (frontend/web)

Storage token:
- `localStorage`
- chiavi: `store:accessToken`, `store:refreshToken`

Uso API protette:
- Authorization header `Bearer <accessToken>` per endpoint `/me` e derivati.

Logout:
- chiama endpoint logout con refresh token.
- poi rimuove token locali.

Nota operativa:
- non c'e un meccanismo globale centralizzato di auto-refresh trasparente per tutte le richieste; i token vengono gestiti dal codice UI dove necessario.

## Flussi guest (utente anonimo)

Il sistema supporta anche checkout guest senza JWT customer.

Regole operative:
- il frontend guest deve generare un identificatore stabile `anonymousId` (UUID) e riutilizzarlo per tutta la sessione guest;
- per endpoint store che richiedono ownership senza JWT, l'identita guest passa via header `X-Anonymous-Id: <uuid>`;
- per la creazione ordine guest, `anonymousId` e valorizzato nel contratto ordine e usato per ownership su letture/azioni guest.

Endpoint store compatibili con guest:
- Cart: add/remove/get (ownership su `X-Anonymous-Id` o JWT customer)
- Order: create/list/get/manual-cancel (ownership su `X-Anonymous-Id` o JWT customer)
- Payment: get session by order/session (ownership su `X-Anonymous-Id` o JWT customer)
- Shipping: get shipment by order (ownership su `X-Anonymous-Id` o JWT customer)

Nota sicurezza guest:
- `X-Anonymous-Id` non equivale a una identita forte come JWT; e un trade-off per supportare guest checkout.
- in produzione, mantenere UUID ad alta entropia e non esporlo inutilmente.

## Frontend Backoffice (frontend/admin)

Login:
- form server-side Astro (`/login`) chiama `validateCredentials` su gateway admin login.

Cookie auth:
- `bo_access_token`: non `httpOnly`, usato dal browser per aggiungere `Authorization` nelle chiamate API client-side.
- `bo_refresh_token`: `httpOnly`, usato lato server (pagina `/logout`) per revoca sessione.

Middleware:
- `src/middleware.ts` protegge tutte le route tranne `/login`.
- `isAuthenticated(...)` valida exp JWT e realm `admin` dal cookie access.

Chiamate API admin:
- `frontend/admin/src/lib/api.ts` legge `bo_access_token` da cookie client e imposta header `Authorization: Bearer ...`.

Gestione utenti admin e permission:
- la sezione `/admin-users` consente creazione utenti admin con set permission custom.
- e possibile aggiornare successivamente le permission di un admin o ripristinare il profilo default.
- quando le permission di un admin cambiano, le sessioni refresh attive di quell'utente vengono revocate per forzare nuovo login con claim aggiornati.

Refresh automatico centralizzato:
- `frontend/admin/src/lib/api.ts` gestisce in modo centralizzato `401` con retry singolo.
- al primo `401` su una request: chiama `POST /api/auth/refresh` e, se ok, ripete una sola volta la request originale.
- `frontend/admin/src/pages/api/auth/refresh.ts` usa `bo_refresh_token` (httpOnly) per invocare `/api/admin/account/v1/users/refresh` e aggiornare i cookie auth.
- in caso di refresh fallito: cookie auth puliti e redirect a login lato client.

Logout:
- pagina `/logout` legge refresh cookie, chiama revoke, poi cancella cookie e redirige a `/login`.

Session timeout UI:
- nel layout backoffice e presente logout automatico per inattivita (redirect a `/logout`).

## Configurazione tecnica

Chiavi principali (`AccountTechnicalOptions`):
- `ConnectionStrings:AccountDb`
- `Account:Jwt:SigningKey`
- `Account:Jwt:AccessTokenMinutes`
- `Account:Jwt:RefreshTokenDays`
- `Account:Jwt:CustomerIssuer`
- `Account:Jwt:CustomerAudience`
- `Account:Jwt:AdminIssuer`
- `Account:Jwt:AdminAudience`
- `Account:OrderApiBaseUrl`
- `Account:OrderInternalApiKey`
- `Account:Admin:Username`
- `Account:Admin:Password`

Chiavi principali (Order API):
- `Order:InternalApiKey`

Uso interno claim guest orders:
- `Account` usa endpoint interno `POST /internal/v1/orders/claim-guest` con header `X-Internal-Api-Key`.
- endpoint non destinato al frontend pubblico.

Bootstrap admin default:
- all'avvio modulo Account viene eseguito `EnsureDefaultAdminAsync(...)`.
- se non esiste admin con username configurato, viene creato.

## Sicurezza: stato attuale e note

Punti buoni gia implementati:
- password hashate
- refresh token hashati in DB
- rotazione refresh token
- revoca sessione su logout e su reset password
- separazione issuer/audience per realm
- policy autorizzative admin granulari per permission
- `codePreview` disponibile solo in ambiente development
- audit trail auth tramite log strutturati per login/refresh/logout/reset password (success/failure)

Trade-off/limiti attuali:
- Storefront usa `localStorage` per token (rischio XSS -> token exposure).
- Backoffice usa access token cookie non-httpOnly per poterlo leggere da JS e inviare Authorization header.
- Il backoffice non e ancora migrato a BFF completo con access token `httpOnly` e forward server-side.

## Troubleshooting rapido

- `401` su endpoint protetti:
- verificare `Authorization` header
- verificare claim `realm` coerente con policy endpoint
- verificare `exp` token

- refresh non funziona:
- token refresh mancante/scaduto/revocato
- realm non coerente con sessione
- fallimento endpoint interno backoffice `POST /api/auth/refresh`

- utente admin non accede al backoffice:
- cookie `bo_access_token` assente o scaduto
- claim `realm` diverso da `admin`

- utente customer non vede dati profilo:
- chiamata senza Bearer token store
- token con realm non customer

## Evoluzioni consigliate

1. Spostare il backoffice a BFF completo con access token `httpOnly` e forward server-side, eliminando lettura token da JS.
2. Valutare enforcement CSRF dedicato anche sul percorso di refresh backoffice (`/api/auth/refresh`).
3. Aggiungere alerting operativo su pattern anomali nei log audit auth (es. picchi `invalid_credentials` / `invalid_refresh_token`).
