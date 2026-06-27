# Collaborative Shopping List

Full-stack grocery list app with real-time sync (SignalR), shared lists via 11-character codes, and OCR ingredient import from recipe screenshots.

**Stack:** React (Vite) + TypeScript + Tailwind · .NET 10 Web API · PostgreSQL · EF Core · Tesseract OCR

## Quick setup

### 1. Prerequisites

| Tool | Notes |
|------|--------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Or retarget projects to `net8.0` / `net9.0` |
| [Node.js LTS](https://nodejs.org/) | For the React client (`npm`) |
| PostgreSQL 14+ | Create an empty database (e.g. `shopping_list`) |

### 2. Clone and configure

```powershell
git clone <your-repo-url>
cd ShoppingListApp
```

**API secrets** — copy the example and fill in your Postgres password and a random JWT key:

```powershell
copy src\ShoppingList.Api\appsettings.Development.local.json.example src\ShoppingList.Api\appsettings.Development.local.json
```

Edit `appsettings.Development.local.json` (this file is gitignored). Alternatively, update `ConnectionStrings:DefaultConnection` in `appsettings.json`.

**Frontend** — copy the env file (optional; defaults match the dev seeder):

```powershell
copy client\.env.example client\.env
```

### 3. Database

Migrations run automatically on API startup in Development. To apply manually:

```powershell
dotnet ef database update --project src/ShoppingList.Infrastructure --startup-project src/ShoppingList.Api
```

### 4. OCR data (optional, for screenshot import)

```powershell
.\scripts\download-tessdata.ps1
```

Downloads `eng.traineddata` into `./tessdata` (gitignored; not committed).

### 5. Run

**Terminal 1 — API**

```powershell
cd src\ShoppingList.Api
dotnet run
```

Swagger: http://localhost:5294/swagger

**Terminal 2 — frontend**

```powershell
cd client
npm install
npm run dev
```

App: http://localhost:5173

> **Tip:** If `dotnet run` fails with “file is being used by another process”, stop the existing API first (`Ctrl+C` or `Get-Process ShoppingList.Api | Stop-Process`).

---

## Project structure

```
src/
  ShoppingList.Domain/        # Entities, enums
  ShoppingList.Application/   # DTOs, interfaces, share-code generator
  ShoppingList.Infrastructure/# EF Core, OCR, migrations
  ShoppingList.Api/           # REST API, SignalR hub
client/                       # React SPA
scripts/                      # Setup helpers (tessdata download)
tests/                        # Unit tests
```

## Features

- **Landing page** — create lists, join with an 11-character share code, view active and archived lists
- **List detail** — add/edit/remove items, real-time updates across tabs, copy share code
- **Archive** — move finished lists to read-only archive (name still editable)
- **OCR upload** — import ingredients from recipe screenshots

### Demo user (Development)

Until auth is implemented, the API uses the `X-User-Id` header. The seeded demo user ID is:

`22222222-2222-2222-2222-222222222222`

Set via `client/.env` → `VITE_USER_ID`.

## API overview

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/lists` | My shared + archived lists |
| POST | `/api/lists` | Create list (returns share code) |
| POST | `/api/lists/join` | Join by share code |
| GET | `/api/lists/{listId}` | List detail + items |
| PATCH | `/api/lists/{listId}` | Rename list |
| POST | `/api/lists/{listId}/archive` | Archive list |
| POST | `/api/lists/{listId}/items` | Add item |
| PATCH | `/api/lists/{listId}/items/{itemId}` | Update / toggle item |
| DELETE | `/api/lists/{listId}/items/{itemId}` | Remove item |
| POST | `/api/lists/{listId}/upload-image` | OCR import (`image` form field) |

SignalR hub: `/hubs/shopping-list` — join with `JoinList(listId)`; events: `ItemAdded`, `ItemUpdated`, `ItemToggled`, `ItemDeleted`, `ItemsBulkAdded`.

## Tests

```powershell
dotnet test
```

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `relation "shopping_lists" does not exist` | Run `dotnet ef database update` (see step 3) |
| `column "ArchivedAt" does not exist` | Same — pending migration not applied |
| `dotnet run` build / file lock errors | Stop the running `ShoppingList.Api` process first |
| OCR upload 503 | Run `.\scripts\download-tessdata.ps1` |
| Create list does nothing | Ensure API is running; check browser console / error banner |
