# Cook In Shop Out

Shop together and cook together — shared grocery lists, saved recipes, and real-time collaboration.

**Stack:** React 19 · Vite · TypeScript · Tailwind CSS · React Router 8 · .NET 10 · PostgreSQL · EF Core · SignalR · Tesseract OCR

## Table of contents

- [Features](#features)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [Authentication and security](#authentication-and-security)
- [Sharing model](#sharing-model)
- [Frontend routes](#frontend-routes)
- [API reference](#api-reference)
- [Real-time updates (SignalR)](#real-time-updates-signalr)
- [Project structure](#project-structure)
- [Development notes](#development-notes)
- [Production deployment](#production-deployment)
- [Tests](#tests)
- [Troubleshooting](#troubleshooting)

## Features

### Accounts and profile
- Register with email, password, preferred name, optional phone and gender
- Email verification required before sign-in
- JWT session via **httpOnly** cookie (`auth_token`) plus a double-submit **CSRF** cookie
- Edit profile (email, phone, preferred name, gender) and view your **friend code**
- Forgot / reset password and change password (invalidates other sessions)

### Friends
- Send friend requests by **email**, **phone number**, or **friend code**
- Accept or decline incoming requests; remove friends

### Shopping lists
- Create, rename, and delete lists (owners only)
- **Share with friends** — invitations must be accepted before the list appears
- Collaborate on active lists in real time (add, edit, check, delete items)
- **Archive** finished lists (owner only) — archived lists are read-only except for renaming
- **Leave** a shared list you do not own
- Import ingredients from a saved recipe into a list
- Category grouping and “check all in category”

### Recipes
- Create and edit recipes with **sectioned ingredients**, cooking steps, and structured content JSON
- Assign a **recipe type** (Main Course, Side Dish, Snack, Dessert, Drink) for filtering
- Search and filter recipes by name and type
- **Share with friends** — same accept/decline flow as lists
- Upload images for OCR ingredient/step extraction (Tesseract)
- Import a recipe’s ingredients into any accessible shopping list

## Quick start

### Prerequisites

| Tool | Version / notes |
|------|-----------------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.x (projects target `net10.0`) |
| [Node.js](https://nodejs.org/) | LTS — for the React client |
| PostgreSQL | 14+ — create an empty database, e.g. `shopping_list` |

### 1. Clone and configure

```powershell
git clone <your-repo-url>
cd ShoppingListApp
```

**API** — copy local secrets (gitignored):

```powershell
copy src\ShoppingList.Api\appsettings.Development.local.json.example src\ShoppingList.Api\appsettings.Development.local.json
```

Edit `appsettings.Development.local.json` with your Postgres password and a JWT signing key (**at least 32 characters**). Do not put secrets in committed `appsettings.json`.

**Client** (optional):

```powershell
copy client\.env.example client\.env
```

In local development the Vite proxy forwards `/api` and `/hubs` to the API, so `VITE_API_URL` is usually not needed.

### 2. Database

Migrations apply automatically when the API starts in **Development**. To run them manually:

```powershell
dotnet ef database update --project src/ShoppingList.Infrastructure --startup-project src/ShoppingList.Api
```

There is **no demo seed data**. Create an account through the app after both servers are running.

### 3. OCR data (optional)

Required only for recipe image import:

```powershell
.\scripts\download-tessdata.ps1
```

This downloads `eng.traineddata` into `./tessdata` (gitignored).

### 4. Run

**Terminal 1 — API**

```powershell
cd src\ShoppingList.Api
dotnet run
```

- API: http://localhost:5294
- Swagger: http://localhost:5294/swagger (Development + `Swagger:Enabled` only)

**Terminal 2 — client**

```powershell
cd client
npm install
npm run dev
```

- App: http://localhost:5173

### 5. First login

1. Open http://localhost:5173/register and create an account.
2. Check the **API console** for the verification link (see [Authentication and security](#authentication-and-security)).
3. Open the link, then sign in at `/login`.

## Configuration

### API (`appsettings*.json` / `*.local.json`)

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Jwt:Issuer` / `Jwt:Audience` | JWT issuer and audience (defaults: `ShoppingListApp`) |
| `Jwt:Key` | Signing secret — **required**, min 32 UTF-8 bytes |
| `Jwt:ExpiryMinutes` | Session lifetime (default `120`) |
| `Cors:AllowedOrigins` | Frontend origin(s), default `http://localhost:5173` |
| `App:FrontendBaseUrl` | Base URL for email verification / reset links |
| `AllowedHosts` | Host header allowlist |
| `Swagger:Enabled` | Swagger UI (only honored in Development; must be `false` in Production) |
| `ForwardedHeaders:KnownProxies` | Trusted reverse-proxy IPs (Production behind a load balancer) |
| `ForwardedHeaders:KnownNetworks` | Trusted proxy CIDR networks (optional) |
| `Email:*` | SMTP settings for production email |
| `Tesseract:DataPath` | Path to tessdata folder (default `./tessdata`) |
| `Tesseract:Language` | OCR language (default `eng`) |

- Local overrides: `appsettings.Development.local.json` (see `.example`)
- Production secrets: `appsettings.Production.local.json` (see `.example`)

### Client (`client/.env`)

| Variable | Purpose |
|----------|---------|
| `VITE_API_URL` | API base URL when **not** using the Vite proxy (production builds). Leave empty for `npm run dev`. |

CSP `connect-src` is injected at build time from `VITE_API_URL` (or same-origin + local WS when unset).

## Authentication and security

### Session flow

1. **Register** — `POST /api/auth/register` creates a user with `EmailVerified = false`.
2. **Verify** — link goes to `/verify-email#token=…`, which calls `POST /api/auth/verify-email`.
3. **CSRF** — client calls `GET /api/auth/csrf` to receive a random token (also set as non-httpOnly `csrf_token` cookie). Mutating cookie-authenticated requests must send matching `X-CSRF`.
4. **Login** — `POST /api/auth/login` sets httpOnly `auth_token` and refreshes CSRF. Login is blocked until email is verified (same generic error as wrong password).
5. **Forgot / reset password** — tokens expire after 1 hour, are single-use, and reset also marks email verified and rotates the security stamp.
6. **Change password / email** — rotates security stamp (other sessions die). Password change re-issues the current browser’s auth cookie.
7. **Logout** — clears cookies and persists a hashed JWT denylist entry (works across API instances).

### Development email

`DevelopmentEmailSender` does **not** send real email. After registration or password reset, look in the API console:

```
[DEV EMAIL] Verification email for user@example.com (Name)
Link: http://localhost:5173/verify-email#token=...
```

### Auth requirements

- Endpoints are authenticated by default (`FallbackPolicy`). Only auth/register/login/verify/forgot/reset/csrf are anonymous.
- Session cookie or `Authorization: Bearer` is accepted. The React client uses cookies + `credentials: "include"`; SignalR uses `withCredentials` and sends `X-CSRF` on negotiate.
- In Production, auth cookies use `SameSite=Strict` and `Secure`.

### Other protections (high level)

- Swagger only when Development **and** `Swagger:Enabled`; otherwise `/swagger*` returns 404
- Origin allowlist for browser `Origin` headers
- Rate limits on auth, friend lookup, and OCR (keyed on real client IP after trusted forwarded headers)
- OCR upload: size limit, MIME allowlist, magic-byte check, concurrency gate
- Production startup validation rejects placeholder CORS/hosts/frontend URLs and enabled Swagger

## Sharing model

Lists and recipes use the same friend-sharing pattern:

1. **Owner** shares with accepted friends (`POST …/shares` with `friendUserIds`).
2. Friend receives a **pending** invitation (`pendingShares` on list/recipe GETs).
3. Friend **accepts** or **declines**.
4. Only **accepted** shares grant access.

Shared grocery lists can be **left** by non-owners (`POST /api/lists/{listId}/leave`).

## Frontend routes

| Path | Auth | Description |
|------|------|-------------|
| `/` | Public | Home — recent lists when signed in |
| `/login` | Public | Sign in |
| `/register` | Public | Create account |
| `/verify-email` | Public | Email verification handler |
| `/forgot-password` | Public | Request a password reset link |
| `/reset-password` | Public | Set a new password from email link |
| `/profile` | Required | Edit profile, friend code, change password |
| `/friends` | Required | Friends and requests |
| `/lists` | Required | Active, archived, and pending list shares |
| `/lists/:listId` | Required | List detail (real-time) |
| `/recipes` | Required | Recipes (search/filter) and pending shares |
| `/recipes/:recipeId` | Required | Recipe view |
| `/recipes/:recipeId/edit` | Required | Recipe edit (sections, steps, OCR) |

## API reference

Unless noted, endpoints require a valid session. JSON uses **camelCase**. Cookie-authenticated `POST`/`PUT`/`PATCH`/`DELETE` (and SignalR negotiate) require a valid `X-CSRF` header.

### Auth

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/auth/csrf` | Public | Issue CSRF cookie + `{ csrfToken }` |
| POST | `/api/auth/register` | Public | Create account |
| POST | `/api/auth/login` | Public | Sign in → `{ user }` (sets cookies) |
| POST | `/api/auth/logout` | Required | Clear session + revoke JWT |
| POST | `/api/auth/verify-email` | Public | `{ token }` |
| POST | `/api/auth/forgot-password` | Public | `{ email }` |
| POST | `/api/auth/reset-password` | Public | `{ token, password }` |

### Users

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users/me` | Current user profile |
| PATCH | `/api/users/me` | Update profile |
| POST | `/api/users/me/change-password` | `{ currentPassword, newPassword }` |

### Friends

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/friends` | Friends, incoming and outgoing requests |
| POST | `/api/friends/requests` | Send request (`email`, `phoneNumber`, or `friendCode`) |
| POST | `/api/friends/requests/{id}/accept` | Accept request |
| POST | `/api/friends/requests/{id}/decline` | Decline request |
| DELETE | `/api/friends/{friendUserId}` | Remove friend |

### Shopping lists

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/lists` | Active lists, archived lists, `pendingShares` |
| POST | `/api/lists` | Create list |
| GET | `/api/lists/{listId}` | List detail and items |
| PATCH | `/api/lists/{listId}` | Rename list |
| DELETE | `/api/lists/{listId}` | Delete list (owner only) |
| POST | `/api/lists/{listId}/archive` | Archive list (owner only) |
| POST | `/api/lists/{listId}/leave` | Leave shared list (non-owner) |
| POST | `/api/lists/{listId}/shares` | Share with friends `{ friendUserIds }` |
| POST | `/api/lists/shares/{permissionId}/accept` | Accept list invitation |
| POST | `/api/lists/shares/{permissionId}/decline` | Decline list invitation |
| POST | `/api/lists/{listId}/items` | Add item |
| PATCH | `/api/lists/{listId}/items/{itemId}` | Update / toggle item |
| DELETE | `/api/lists/{listId}/items/{itemId}` | Remove item |
| POST | `/api/lists/{listId}/items/check-all` | Check/uncheck all (optional category filter) |
| POST | `/api/lists/{listId}/items/delete-many` | Bulk delete `{ itemIds }` |
| POST | `/api/lists/{listId}/import-recipe/{recipeId}` | Add recipe ingredients to list |

### Recipes

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/recipes` | Recipes and `pendingShares` |
| POST | `/api/recipes` | Create recipe (optional `recipeType`) |
| GET | `/api/recipes/{recipeId}` | Recipe detail |
| PATCH | `/api/recipes/{recipeId}` | Update name and/or `recipeType` |
| DELETE | `/api/recipes/{recipeId}` | Delete recipe (owner only) |
| POST | `/api/recipes/{recipeId}/shares` | Share with friends |
| POST | `/api/recipes/shares/{permissionId}/accept` | Accept recipe invitation |
| POST | `/api/recipes/shares/{permissionId}/decline` | Decline recipe invitation |
| POST | `/api/recipes/{recipeId}/ingredients` | Add ingredient |
| PATCH | `/api/recipes/{recipeId}/ingredients/{ingredientId}` | Update ingredient |
| POST | `/api/recipes/{recipeId}/ingredients/rename-section` | Rename an ingredient section |
| DELETE | `/api/recipes/{recipeId}/ingredients/{ingredientId}` | Remove ingredient |
| POST | `/api/recipes/{recipeId}/ingredients/delete-many` | Bulk delete `{ ingredientIds }` |
| PUT | `/api/recipes/{recipeId}/steps` | Replace cooking steps |
| PUT | `/api/recipes/{recipeId}/content` | Save structured recipe JSON |
| POST | `/api/recipes/{recipeId}/upload-image` | OCR import (`multipart/form-data`: `image`, optional `importMode`) |

`importMode` values: `FullRecipeWithSteps`, `IngredientsOnly`, `CookingStepsOnly`.

`recipeType` values: `Main Course`, `Side Dish`, `Snack`, `Dessert`, `Drink`.

Interactive docs: http://localhost:5294/swagger (Development only when enabled).

## Real-time updates (SignalR)

**Hub:** `/hubs/shopping-list` (requires session cookie or Bearer token + CSRF on HTTP negotiate)

**Client → server**

| Method | Description |
|--------|-------------|
| `JoinList(listId)` | Subscribe to list updates (access-checked) |
| `LeaveList(listId)` | Unsubscribe |

**Server → client** (after joining a list group)

| Event | Payload |
|-------|---------|
| `ItemAdded` | Item DTO |
| `ItemUpdated` | Item DTO |
| `ItemToggled` | `itemId`, `isChecked` |
| `ItemDeleted` | `itemId` |
| `ItemsBulkAdded` | Item DTO array |
| `ItemsBulkToggled` | `itemIds`, `isChecked` |
| `ItemsBulkDeleted` | `itemIds` |

The React client connects on the list detail page and reconnects automatically. Revoked share access removes the user from the hub group.

## Project structure

```
ShoppingListApp/
├── client/                 # React SPA (Vite)
│   └── src/
│       ├── api/            # REST client wrappers
│       ├── components/     # UI components
│       ├── context/        # Auth + shopping list state
│       ├── lib/            # apiClient, CSRF, SignalR, validation helpers
│       ├── pages/          # Route pages
│       └── types/          # Shared DTOs
├── src/
│   ├── ShoppingList.Domain/          # Entities, enums
│   ├── ShoppingList.Application/     # Validators, DTOs, recipe helpers, OCR interface
│   ├── ShoppingList.Infrastructure/  # EF Core, migrations, Tesseract OCR
│   └── ShoppingList.Api/             # Controllers, hubs, auth, security middleware
├── scripts/                # tessdata download, dependency audit
├── tests/                  # Unit tests
└── tools/SchemaRepair/     # One-off DB schema repair utility
```

## Development notes

### EF Core migrations

```powershell
dotnet ef migrations add <Name> --project src/ShoppingList.Infrastructure --startup-project src/ShoppingList.Api
```

### Schema repair

If an older database is missing columns the initializer expects, the API can run repair SQL on startup in Development. For manual repair:

```powershell
dotnet run --project tools/SchemaRepair
```

Requires `appsettings.Development.local.json` with a valid connection string.

### Dependency audit

```powershell
.\scripts\audit-deps.ps1
```

## Production deployment

1. **Secrets file** (gitignored):

```powershell
copy src\ShoppingList.Api\appsettings.Production.local.json.example src\ShoppingList.Api\appsettings.Production.local.json
```

Set at least:

| Setting | Notes |
|---------|--------|
| `ConnectionStrings:DefaultConnection` | Production Postgres |
| `Jwt:Key` | Long random secret (≥ 32 chars) |
| `Cors:AllowedOrigins` | Real `https://…` frontend origin(s) — not localhost/placeholders |
| `App:FrontendBaseUrl` | Same frontend origin for email links |
| `AllowedHosts` | Real API hostname(s) |
| `Email:*` | SMTP for verification / reset mail |
| `ForwardedHeaders:KnownProxies` or `KnownNetworks` | Required for correct client IPs **if** behind a reverse proxy / load balancer |

2. **Environment:** `ASPNETCORE_ENVIRONMENT=Production`. The API **refuses to start** if Swagger is enabled or CORS/hosts/frontend URLs look like placeholders, localhost, or non-HTTPS.

3. **Migrations:** apply explicitly in Production (they do **not** auto-run outside Development):

```powershell
dotnet ef database update --project src/ShoppingList.Infrastructure --startup-project src/ShoppingList.Api
```

4. **Publish API** and host behind HTTPS (prefer terminating TLS at a reverse proxy, then set KnownProxies).

5. **Build client** with the API origin (injects CSP `connect-src`):

```powershell
cd client
$env:VITE_API_URL="https://api.your-real-domain.com"
npm run build
```

Serve `client/dist` from your static host or the same reverse proxy. If the browser talks to the API on another origin, that origin must be in `Cors:AllowedOrigins`.

6. Run `.\scripts\audit-deps.ps1` before deploy.

## Tests

```powershell
dotnet test
```

Includes API and Infrastructure unit tests (hub tracking, OCR/ingredient parsing, recipe content helpers).

## Troubleshooting

| Problem | Likely cause | Fix |
|---------|--------------|-----|
| `relation "shopping_lists" does not exist` | Migrations not applied | `dotnet ef database update` or restart API in Development |
| `Jwt:Key is not configured` / key too short | Missing or weak JWT secret | Set `Jwt:Key` (≥ 32 chars) in `.local.json` |
| API won't start in Production | Placeholder CORS/hosts/Swagger | Fix `appsettings.Production.local.json` per [Production deployment](#production-deployment) |
| 403 `Invalid or missing CSRF token` | Missing/stale `X-CSRF` | Ensure client called `/api/auth/csrf` after login; hard-refresh |
| 401 on API calls | Not signed in or revoked session | Sign in again |
| OCR upload 503 / no ingredients | Missing tessdata | Run `.\scripts\download-tessdata.ps1` |
| SignalR disconnected / 403 Origin | API down, auth, or CORS origin mismatch | Check API, login, and `Cors:AllowedOrigins` |
| Rate limits hit everyone equally | Proxy IPs not trusted | Configure `ForwardedHeaders:KnownProxies` |
| `dotnet run` file lock errors | API already running | Stop existing `ShoppingList.Api` process |
| Verification link wrong host | Wrong `App:FrontendBaseUrl` | Match your real client URL |
| Swagger 404 | Not Development or `Swagger:Enabled` false | Enable only in Development `appsettings` |
