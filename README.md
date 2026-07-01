# Cook With Me

Shop together and cook together — shared grocery lists, saved recipes, and real-time collaboration.

**Stack:** React 19 · Vite · TypeScript · Tailwind CSS · .NET 10 · PostgreSQL · EF Core · SignalR · Tesseract OCR

## Table of contents

- [Features](#features)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [Authentication](#authentication)
- [Sharing model](#sharing-model)
- [Frontend routes](#frontend-routes)
- [API reference](#api-reference)
- [Real-time updates (SignalR)](#real-time-updates-signalr)
- [Project structure](#project-structure)
- [Development notes](#development-notes)
- [Tests](#tests)
- [Troubleshooting](#troubleshooting)

## Features

### Accounts and profile
- Register with email, password, preferred name, optional phone and gender
- Email verification required before sign-in
- JWT authentication via **httpOnly cookie** (`auth_token`); session sent automatically on API and SignalR requests
- Edit profile (email, phone, preferred name, gender) and view your **friend code**

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
- Create recipes with ingredients, cooking steps, and structured content (sections)
- **Share with friends** — same accept/decline flow as lists
- Edit or view recipes; upload images for OCR ingredient/step extraction (Tesseract)
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

Edit `appsettings.Development.local.json` with your Postgres password and a JWT signing key (at least 32 characters). Do not put secrets in `appsettings.json` — it is committed to git.

**Client** (optional):

```powershell
copy client\.env.example client\.env
```

In local development the Vite dev server proxies `/api` and `/hubs` to the API, so `VITE_API_URL` is usually not needed.

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
- Swagger: http://localhost:5294/swagger  

**Terminal 2 — client**

```powershell
cd client
npm install
npm run dev
```

- App: http://localhost:5173  

### 5. First login

1. Open http://localhost:5173/register and create an account.
2. Check the **API console** for the verification link (see [Authentication](#authentication)).
3. Open the link, then sign in at `/login`.

## Configuration

### API (`appsettings.json` / `appsettings.Development.local.json`)

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Jwt:Issuer` / `Jwt:Audience` | JWT issuer and audience (defaults: `ShoppingListApp`) |
| `Jwt:Key` | Signing secret — **required**, min ~32 chars |
| `Cors:AllowedOrigins` | Frontend origin(s), default `http://localhost:5173` |
| `App:FrontendBaseUrl` | Base URL for email verification links |
| `AllowedHosts` | Host header allowlist (`localhost` in dev; set your domains in production) |
| `Email:*` | SMTP settings for production email (see `appsettings.Production.local.json.example`) |
| `Tesseract:DataPath` | Path to tessdata folder (default `./tessdata`) |
| `Tesseract:Language` | OCR language (default `eng`) |

Local overrides go in `appsettings.Development.local.json` (see `.example` file). Production secrets go in `appsettings.Production.local.json` (see `.example` file).

### Client (`client/.env`)

| Variable | Purpose |
|----------|---------|
| `VITE_API_URL` | API base URL when **not** using the Vite proxy (e.g. production builds). Leave empty for `npm run dev`. |

## Authentication

1. **Register** — `POST /api/auth/register` creates a user with `EmailVerified = false`.
2. **Verify** — a link is sent to `/verify-email#token=…` on the frontend, which calls `POST /api/auth/verify-email` with the token.
3. **Login** — `POST /api/auth/login` sets an httpOnly session cookie and returns the user profile. Login is blocked until email is verified (same generic error as wrong password).
4. **Forgot password** — `POST /api/auth/forgot-password` with `{ email }` always returns the same message (no account enumeration). If the account exists and is verified, a reset link is sent to `/reset-password#token=…`.
5. **Reset password** — the reset page calls `POST /api/auth/reset-password` with `{ token, password }`. Tokens expire after 1 hour and are single-use.
6. **Logout** — `POST /api/auth/logout` clears the cookie and revokes the session server-side.

### Development email

The API uses `DevelopmentEmailSender`, which **does not send real email**. After registration, look in the API console for a log block like:

```
[DEV EMAIL] Verification email for user@example.com (Name)
Link: http://localhost:5173/verify-email#token=...
```

Copy that link into your browser to verify, then sign in. Password reset emails are logged the same way (`[DEV EMAIL] Password reset email…` with a link to `/reset-password#token=…`).

All routes except `/api/auth/register`, `/api/auth/login`, `/api/auth/verify-email`, `/api/auth/forgot-password`, and `/api/auth/reset-password` require a valid session cookie (or `Authorization: Bearer` for API tools). The React client uses `credentials: "include"`; SignalR uses the same cookie via `withCredentials`.

## Sharing model

Lists and recipes use the same friend-sharing pattern:

1. **Owner** shares with one or more **accepted friends** (`POST …/shares` with `friendUserIds`).
2. Each friend receives a **pending** invitation (`pendingShares` on `GET /api/lists` or `/api/recipes`).
3. The friend **accepts** or **declines** (`POST …/shares/{permissionId}/accept|decline`).
4. Only **accepted** shares grant access. Owners see share counts on list cards (“Shared with N people”).

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
| `/profile` | Required | Edit profile, friend code |
| `/friends` | Required | Friends and requests |
| `/lists` | Required | Active, archived, and pending list shares |
| `/lists/:listId` | Required | List detail (real-time) |
| `/recipes` | Required | Recipes and pending recipe shares |
| `/recipes/:recipeId` | Required | Recipe view |
| `/recipes/:recipeId/edit` | Required | Recipe edit |

## API reference

Unless noted, all endpoints require a valid JWT. JSON bodies use **camelCase**.

### Auth (public)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | Create account |
| POST | `/api/auth/login` | Sign in → `{ user }` (sets httpOnly cookie) |
| POST | `/api/auth/logout` | Clear session cookie |
| POST | `/api/auth/verify-email` | Verify email address (`{ token }`) |
| GET | `/api/auth/verify-email?token=` | Verify email (legacy) |
| POST | `/api/auth/forgot-password` | Request password reset (`{ email }`) |
| POST | `/api/auth/reset-password` | Reset password (`{ token, password }`) |

### Users

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users/me` | Current user profile |
| PATCH | `/api/users/me` | Update profile |

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
| POST | `/api/lists/{listId}/items/check-all` | Check/uncheck all items (optional category filter) |
| POST | `/api/lists/{listId}/items/delete-many` | Bulk delete `{ ingredientIds }` |
| POST | `/api/lists/{listId}/import-recipe/{recipeId}` | Add recipe ingredients to list |

### Recipes

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/recipes` | Recipes and `pendingShares` |
| POST | `/api/recipes` | Create recipe |
| GET | `/api/recipes/{recipeId}` | Recipe detail |
| PATCH | `/api/recipes/{recipeId}` | Rename recipe |
| DELETE | `/api/recipes/{recipeId}` | Delete recipe (owner only) |
| POST | `/api/recipes/{recipeId}/shares` | Share with friends |
| POST | `/api/recipes/shares/{permissionId}/accept` | Accept recipe invitation |
| POST | `/api/recipes/shares/{permissionId}/decline` | Decline recipe invitation |
| POST | `/api/recipes/{recipeId}/ingredients` | Add ingredient |
| DELETE | `/api/recipes/{recipeId}/ingredients/{ingredientId}` | Remove ingredient |
| POST | `/api/recipes/{recipeId}/ingredients/delete-many` | Bulk delete ingredients |
| PUT | `/api/recipes/{recipeId}/steps` | Replace cooking steps |
| PUT | `/api/recipes/{recipeId}/content` | Save structured recipe JSON |
| POST | `/api/recipes/{recipeId}/upload-image` | OCR import (`multipart/form-data`: `image`, optional `importMode`) |

`importMode` values: `FullRecipeWithSteps`, `IngredientsOnly`, `CookingStepsOnly`.

Interactive documentation: http://localhost:5294/swagger (Development only).

## Real-time updates (SignalR)

**Hub:** `/hubs/shopping-list` (requires session cookie or Bearer token)

**Client → server**

| Method | Description |
|--------|-------------|
| `JoinList(listId)` | Subscribe to list updates |
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

The React client connects when you open a list detail page and reconnects automatically.

## Project structure

```
ShoppingListApp/
├── client/                 # React SPA (Vite)
│   └── src/
│       ├── api/            # REST client wrappers
│       ├── components/     # UI components
│       ├── context/        # Auth + shopping list state
│       └── pages/          # Route pages
├── src/
│   ├── ShoppingList.Domain/          # Entities, enums, value objects
│   ├── ShoppingList.Application/     # Parsers, validators, DTOs, recipe builders
│   ├── ShoppingList.Infrastructure/  # EF Core, migrations, Tesseract OCR
│   └── ShoppingList.Api/             # Controllers, hubs, auth, services
├── scripts/                # tessdata download helper
├── tests/                  # Unit tests (Infrastructure)
└── tools/SchemaRepair/     # One-off DB schema repair utility
```

## Development notes

### EF Core migrations

Add a migration after entity changes:

```powershell
dotnet ef migrations add <Name> --project src/ShoppingList.Infrastructure --startup-project src/ShoppingList.Api
```

### Schema repair

If an older database is missing columns the initializer expects, the API runs repair SQL on startup in Development. For manual repair:

```powershell
dotnet run --project tools/SchemaRepair
```

Requires `appsettings.Development.local.json` with a valid connection string.

### Production build (client)

```powershell
cd client
npm run build
```

### Production deployment

1. Copy and edit production secrets:

```powershell
copy src\ShoppingList.Api\appsettings.Production.local.json.example src\ShoppingList.Api\appsettings.Production.local.json
```

Set `ConnectionStrings`, `Jwt:Key`, `Cors:AllowedOrigins`, `App:FrontendBaseUrl`, `AllowedHosts`, and `Email` (SMTP) values.

2. Build and publish the API with `ASPNETCORE_ENVIRONMENT=Production`.

3. Build the client with `VITE_API_URL` set to your API origin (CSP `connect-src` is injected at build time):

```powershell
cd client
$env:VITE_API_URL="https://api.your-domain.com"
npm run build
```

4. Run dependency audits before deploy:

```powershell
.\scripts\audit-deps.ps1
```

### CORS

When deploying, add your frontend URL to `Cors:AllowedOrigins` and set `App:FrontendBaseUrl` for verification emails.

## Tests

```powershell
dotnet test
```

Covers ingredient line parsing and recipe content building.

## Troubleshooting

| Problem | Likely cause | Fix |
|---------|--------------|-----|
| `relation "shopping_lists" does not exist` | Migrations not applied | `dotnet ef database update` or restart API in Development |
| `Jwt:Key is not configured` | Missing JWT secret | Set `Jwt:Key` in appsettings or `.local.json` |
| 401 on API calls | Not signed in or expired session | Sign in again |
| OCR upload 503 / no ingredients | Missing tessdata | Run `.\scripts\download-tessdata.ps1` |
| SignalR disconnected | API not running or auth missing | Ensure API is up and you are logged in |
| `dotnet run` file lock errors | API already running | Stop existing `ShoppingList.Api` process |
| Verification link does nothing | Wrong `App:FrontendBaseUrl` | Match your client URL in appsettings |
