# PostgreSQL Migration Guide

SourceFlow uses **PostgreSQL** as the primary database. SQLite has been removed.

## Environment Variable

Set the connection string via `DATABASE_URL`:

```
DATABASE_URL=postgresql://USER:PASSWORD@HOST:5432/DB_NAME
```

Examples:
- Local: `postgresql://postgres:postgres@localhost:5432/sourceflow`
- Heroku/Render: Use the `DATABASE_URL` provided by the platform (often `postgres://` — automatically converted)

## Dependency Installation

```bash
cd backend/SourceFlow.Api
dotnet restore
```

**Packages:**
- `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.0
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 8.0.0
- SQLite package removed

## Migration Commands

```bash
cd backend/SourceFlow.Api

# Generate a new migration (after model changes)
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Or let the app auto-apply on startup (already configured in Program.cs)
```

## Run Locally

### 1. Start PostgreSQL

**Option A: Docker Compose**
```bash
docker compose up -d postgres
```

**Option B: Local PostgreSQL**
- Ensure PostgreSQL 14+ is running
- Create database: `createdb sourceflow`

### 2. Set Connection String

**Option A: Environment variable**
```bash
export DATABASE_URL=postgresql://postgres:postgres@localhost:5432/sourceflow
```

**Option B: appsettings.Development.json** (already configured for local Docker)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=sourceflow;Username=postgres;Password=postgres"
  }
}
```

### 3. Run the API

```bash
cd backend/SourceFlow.Api
dotnet run
```

Migrations run automatically on startup. Plans are seeded if empty.

## Database Schema (PostgreSQL)

| Table | Purpose |
|-------|---------|
| Users | Auth, credits, Razorpay customer ID |
| Jobs | Job descriptions per user |
| Plans | Subscription tiers (Starter, Growth, Pro) |
| Payments | Payment records (Razorpay) |
| CreditTransactions | Credit deductions and purchases |
| ProfileAnalysisCache | Cached scan results |
| ShortlistedCandidates | User shortlist per job |

**PostgreSQL types used:**
- `integer` for IDs and counts
- `numeric` for decimal (Amount, Price)
- `text` for strings
- `timestamp with time zone` for timestamps

## Connection Pooling

Npgsql enables connection pooling by default. To tune:

```
DATABASE_URL=postgresql://...?Pooling=true&MinPoolSize=2&MaxPoolSize=100
```

## Health Check

`GET /health` includes a database connectivity check. Returns `Healthy` when PostgreSQL is reachable.

## Verification

- **Auth:** POST `/auth/register`, POST `/auth/login`
- **Razorpay webhook:** POST `/payments/razorpay-webhook`
- **Razorpay webhooks:** POST `/payments/razorpay/webhook`
- **Extension APIs:** All endpoints use the same DbContext
