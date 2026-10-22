# WCX Time-Off Management System

Full-stack exam application for agent time-off credits, requests, and admin approval.

The Angular UI talks to an ASP.NET Core Web API. MongoDB is the source of truth for users, credit balances, and time-off requests. Available balance is always `EarnedCredits - TakenCredits` on the server; the UI displays that value and does not invent dashboard data.

## Architecture

```
wcx-timeoff/
  backend/src/WcxTimeOff.Api/     ASP.NET Core 9 Web API
  backend/tests/WcxTimeOff.Api.Tests/
  frontend/                       Angular 19 standalone app
  docker-compose.yml              MongoDB 7 for local development
```

Backend layers:

- `Controllers` — HTTP, auth attributes, status codes
- `Services` — login, credit edits, request create/approve/deny
- `Data` — MongoDB context, indexes, repositories
- `Domain` — entities and hour-calculation rules
- `Contracts` — request/response DTOs
- `Auth` — JWT issuance
- `Middleware` — consistent error JSON

Frontend:

- Route guards for authentication and Admin/Agent roles
- HTTP interceptor that attaches the JWT
- Reactive forms with the same date/hour/overlap/balance rules as the API
- Typed `ApiService` for every exam endpoint

## Technology stack

| Piece | Version |
| --- | --- |
| .NET | 9.0 |
| ASP.NET Core JWT Bearer | 9.0.9 |
| MongoDB.Driver | 3.4.3 |
| Swashbuckle | 8.1.1 |
| xUnit / FluentAssertions | 2.9.3 / 8.2.0 |
| Angular | 19.2 |
| TypeScript | 5.7 |
| Node.js (dev) | 24 LTS or 22 LTS |
| MongoDB | 7.0 |

## Prerequisites

- .NET 9 SDK
- Node.js 22+ and npm
- MongoDB 7 listening on `mongodb://localhost:27017`, or Docker for `docker compose up -d`

## MongoDB setup

```bash
docker compose up -d
```

Or install MongoDB Community and keep the default port `27017`. If your deployment is a standalone node, multi-document transactions are not used. Approvals instead:

1. Atomically claim a request only while `Status == Pending`.
2. Atomically increment `TakenCredits` only when `EarnedCredits >= TakenCredits + hours`.
3. If the credit update fails, revert the request to `Pending`.

That prevents double approval without requiring a replica set.

## Environment configuration

Secrets belong in environment variables or user secrets, not in git.

| Variable | Purpose |
| --- | --- |
| `JWT__KEY` | HMAC signing key (required in non-Development) |
| `Mongo__ConnectionString` | MongoDB URI |
| `Mongo__DatabaseName` | Database name (`wcx_timeoff`) |

In Development, if `Jwt:Key` is empty the API uses a **dev-only** fallback key. Override it before any shared environment. See `.env.example`.

`appsettings.json` contains no production passwords. Seed passwords below are development-only.

## Development seed (development-only)

Enabled when `Seed:Enabled` is true (default). It runs once on an empty database and creates:

| Role | Email | Password | Starting earned hours |
| --- | --- | --- | --- |
| Admin | `admin@wcx.local` | `Admin123!` | — |
| Agent | `jordan@wcx.local` | `Agent123!` | 80 |
| Agent | `sam@wcx.local` | `Agent123!` | 40 |
| Agent | `riley@wcx.local` | `Agent123!` | 16 |

Do not reuse these credentials outside local demo.

## Run the backend

```bash
cd backend/src/WcxTimeOff.Api
dotnet run
```

API: http://localhost:5080  
Swagger (Development): http://localhost:5080/swagger

## Run the Angular frontend

```bash
cd frontend
npm install
npm start
```

UI: http://localhost:4200  
`proxy.conf.json` forwards `/api` to `http://localhost:5080`. Direct calls also use `environment.apiUrl`.

## Automated tests

```bash
dotnet test WcxTimeOff.sln
cd frontend
npm test
```

Backend tests cover balance math, full-day hours, partial-day rules, overlap, insufficient balance, approval incrementing taken credits once, denial leaving taken credits unchanged, and unauthorized request access. Frontend tests cover the shared hour/overlap/balance helpers.

## API

All protected routes require `Authorization: Bearer <token>`. Password hashes are never returned.

| Method | Path | Auth |
| --- | --- | --- |
| POST | `/api/auth/login` | Anonymous |
| GET | `/api/auth/me` | Any authenticated user |
| GET | `/api/me/credits` | Agent (own balance; id comes from the JWT) |
| GET | `/api/admin/agents` | Admin |
| GET | `/api/admin/agents/{id}/credits` | Admin |
| PUT | `/api/admin/agents/{id}/credits` | Admin; body `{ "earnedCredits": number }` |
| POST | `/api/timeoff-requests` | Agent; ownership from JWT, not the body |
| GET | `/api/timeoff-requests/my` | Agent |
| GET | `/api/timeoff-requests/{id}` | Agent (own) or Admin |
| GET | `/api/admin/timeoff-requests?status=Pending` | Admin |
| POST | `/api/admin/timeoff-requests/{id}/approve` | Admin |
| POST | `/api/admin/timeoff-requests/{id}/deny` | Admin |

Error body: `{ "error": "code", "message": "human-readable text" }`.

## Business rules and assumptions

- **Available balance** = earned − taken (approved consumption only).
- **Requestable balance** = earned − taken − sum of *other* pending hours. Create and approval both honor remaining capacity so pending requests cannot collectively overdraw.
- **Full day** = 8 hours × inclusive **calendar** days (`end - start + 1`). No holiday/weekend calendar is applied; `TimeOffCalculator` / `time-off-rules.ts` isolate that so a work calendar can be added later.
- **Partial day** = a single date and a positive hour amount, at most 8.
- Inclusive overlap: `startA <= endB && endA >= startB`. Pending and approved block new requests; denied do not.
- Clients cannot set `TakenCredits` or `AvailableBalance`. Approval is the only path that increments taken credits, and only via an atomic pending claim.
- Agent id on create is taken from the JWT, never from the request body.

## Demonstration walkthrough

1. Start MongoDB, the API, and `npm start`.
2. Sign in as `admin@wcx.local` / `Admin123!`. Confirm three agents and their earned/taken/available columns.
3. Edit Sam’s earned credits. Try a value below taken credits after an approval and confirm the API rejects it.
4. Sign in as `jordan@wcx.local` / `Agent123!`. Submit a full-day request and a partial-day request. Try an overlapping range and a request larger than requestable balance.
5. As admin, open the approval queue. Approve one request and confirm taken credits increase once on the overview. Deny another and confirm taken credits stay the same. Retrying approve/deny on a reviewed request returns a conflict.

## Known limitations

- No working-day calendar (weekends count).
- No email notifications or audit log UI.
- JWT fallback key is Development-only; set `JWT__KEY` everywhere else.
- GitHub publishing is not performed unless you explicitly authorize a push.
- MongoDB must be running; the API seed runs at startup and will fail if the database is unreachable.
- Angular Karma tests need a local Chrome/Edge binary (`CHROME_BIN` if needed).
