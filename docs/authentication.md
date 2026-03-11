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

## Modello di identita

### Realm

Il sistema usa due realm separati:
- `customer`
- `admin`

Il realm e salvato:
- nel claim JWT `realm`
- nella tabella `users`
- nella tabella `refresh_sessions`

### Policy autorizzative API

Policy configurate in `Account.Infrastructure`:
- `CustomerPolicy`: richiede claim `realm=customer`
- `AdminPolicy`: richiede claim `realm=admin`

Endpoint protetti:
- Store profile (`/store/v1/me`, `/me/addresses`, `/me/orders`) -> `CustomerPolicy`
- Admin profile/gestione utenti/clienti (`/admin/v1/me`, `/admins`, `/customers`) -> `AdminPolicy`

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
- `shipping:read`
- `shipping:write`

Nota:
- oggi le policy usano solo `realm`.
- le `permission` sono incluse nel token e disponibili via endpoint `/me/permissions`, ma non ancora usate in policy granulari endpoint-by-endpoint.

## Endpoint Account (versione v1)

Base path interni (Account.Api):
- Store: `/store/v1`
- Admin: `/admin/v1`

Esposti esternamente via Gateway:
- `/api/store/account/v1/...`
- `/api/admin/account/v1/...`

### Store auth

- `POST /api/store/account/v1/users/register`
- `POST /api/store/account/v1/users/login`
- `POST /api/store/account/v1/users/refresh`
- `POST /api/store/account/v1/users/logout`
- `POST /api/store/account/v1/users/verify-email`
- `POST /api/store/account/v1/users/forgot-password`
- `POST /api/store/account/v1/users/reset-password`
- `POST /api/store/account/v1/users/resend-verification`

### Store profilo (protetti)

- `GET /api/store/account/v1/me`
- `PUT /api/store/account/v1/me`
- `GET /api/store/account/v1/me/addresses`
- `POST /api/store/account/v1/me/addresses`
- `PUT /api/store/account/v1/me/addresses/{addressId}`
- `DELETE /api/store/account/v1/me/addresses/{addressId}`
- `GET /api/store/account/v1/me/orders`

### Admin auth

- `POST /api/admin/account/v1/users/login`
- `POST /api/admin/account/v1/users/refresh`
- `POST /api/admin/account/v1/users/logout`

### Admin profilo e gestione (protetti)

- `GET /api/admin/account/v1/me`
- `GET /api/admin/account/v1/me/permissions`
- `GET /api/admin/account/v1/admins`
- `POST /api/admin/account/v1/admins`
- `POST /api/admin/account/v1/admins/{adminUserId}/password/reset`
- `DELETE /api/admin/account/v1/admins/{adminUserId}`
- `GET /api/admin/account/v1/customers`
- `GET /api/admin/account/v1/customers/{customerId}`
- `PUT /api/admin/account/v1/customers/{customerId}`
- `POST /api/admin/account/v1/customers/{customerId}/password/reset`
- `GET /api/admin/account/v1/customers/{customerId}/addresses`
- `POST /api/admin/account/v1/customers/{customerId}/addresses`
- `PUT /api/admin/account/v1/customers/{customerId}/addresses/{addressId}`
- `DELETE /api/admin/account/v1/customers/{customerId}/addresses/{addressId}`

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
- `Account:Admin:Username`
- `Account:Admin:Password`

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

Trade-off/limiti attuali:
- Storefront usa `localStorage` per token (rischio XSS -> token exposure).
- Backoffice usa access token cookie non-httpOnly per poterlo leggere da JS e inviare Authorization header.
- Le permission sono nel token ma le policy endpoint oggi sono basate su realm, non su permission fine-grained.
- Endpoint forgot/reset/verify restituiscono `codePreview` (utile dev/test, da valutare in produzione).

## Troubleshooting rapido

- `401` su endpoint protetti:
- verificare `Authorization` header
- verificare claim `realm` coerente con policy endpoint
- verificare `exp` token

- refresh non funziona:
- token refresh mancante/scaduto/revocato
- realm non coerente con sessione

- utente admin non accede al backoffice:
- cookie `bo_access_token` assente o scaduto
- claim `realm` diverso da `admin`

- utente customer non vede dati profilo:
- chiamata senza Bearer token store
- token con realm non customer

## Evoluzioni consigliate

1. Spostare il backoffice a BFF completo con access token `httpOnly` e forward server-side, eliminando lettura token da JS.
2. Introdurre policy granulari basate su claim `permission` (es. `catalog:write`).
3. Introdurre refresh automatico centralizzato lato frontend con retry singolo su `401`.
4. Disabilitare `codePreview` in ambienti non-dev.
5. Aggiungere audit trail esplicito per login/refresh/logout/reset password.
