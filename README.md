# Support Desk

## Overview

Support Desk is an internal ticket-management tool for creating, assigning, updating, and resolving customer support tickets.

This repository contains:

- an **ASP.NET Core 9** backend that owns the business rules
- an **Angular 20** frontend for day-to-day ticket work

The assignment emphasis is backend correctness: status workflow, assignment rules, due dates, closed-ticket immutability, and safe error handling. The UI mirrors server capabilities; it is not the authority for those rules.

**Stack**

| Layer | Technology |
|-------|------------|
| Backend | .NET 9, ASP.NET Core 9, EF Core 9, SQL Server |
| Frontend | Angular 20, TypeScript, RxJS |
| Tests | xUnit (backend), Jasmine/Karma (frontend) |

---

## Architecture

```text
Angular
  ↓ HTTP /api
ASP.NET Core Controllers
  ↓
Application services (in SupportDesk.Api)
  ↓
Domain policies / entities
  ↓
EF Core / SQL Server
```

### Domain (`SupportDesk.Domain`)

Contains entities, enums, a clock abstraction (`IClock`), and **pure** business policies, for example:

- `TicketTransitionPolicy`
- `TicketAssignmentPolicy`
- `TicketMutability`
- `DueDateCalculator`
- `OverdueEvaluator`

These policies do not touch the database or HTTP.

### Application (`SupportDesk.Api/Application`)

Application services live **inside the Api project** (for example `TicketService`, `AgentQueryService`). For this assignment size, a separate Application class library would add ceremony without much benefit.

Responsibilities:

- orchestrate use cases
- open transactions where needed (ticket create + reference allocation)
- application validation / error mapping
- invoke domain policies
- load contextual data (for example whether an assigned agent is active)
- map entities to DTOs

### Infrastructure (`SupportDesk.Infrastructure`)

- `SupportDeskDbContext` and EF configurations
- migrations
- Development seed data
- `SqlServerTicketReferenceGenerator` (per-year counter with SQL Server locking)

### Angular (`frontend/support-desk`)

Feature-based folders (`tickets/`, `agents/`, `core/`, `shared/`). `TicketService` and `AgentService` isolate `HttpClient`. The UI uses server-provided capability flags and `allowedTransitions`. Angular does **not** re-implement the status state machine or authoritative due-date / overdue logic.

---

## Prerequisites

### Backend

- .NET SDK **9.x** (projects target `net9.0`)

### Frontend

- **Node.js 22.x** (verified with Node 22.20.0)
- npm
- Angular CLI **20.x** (project uses `@angular/cli` ^20.3.6; `npx ng` is fine)

### Database

- **Microsoft SQL Server** via the EF Core SQL Server provider
- **Verified development environment:** SQL Server **2019 Developer** on `localhost` with **Windows authentication**

Notes:

- The assignment also allows LocalDB. This provider can talk to LocalDB where it is installed by adjusting the connection string.
- This implementation was **verified against SQL Server 2019 Developer on `localhost`**, not LocalDB.
- Docker is **not** required.
- PostgreSQL is **not** used by this implementation.

---

## Project Structure

```text
backend/
  SupportDesk.Api/             # HTTP API, application services, Program.cs
  SupportDesk.Domain/          # Entities, enums, pure policies
  SupportDesk.Infrastructure/  # EF Core, migrations, seed, reference generator
  SupportDesk.Tests/           # xUnit domain / persistence / application / API tests
  SupportDesk.sln
  .config/dotnet-tools.json    # pinned dotnet-ef 9.0.19

frontend/
  support-desk/                # Angular 20 standalone app

README.md
```

---

## Database Setup

Connection string configuration key:

```text
ConnectionStrings:SupportDesk
```

**Verified Development example** (also the design-time default):

```text
Server=localhost;Database=SupportDesk;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Defined in:

- `backend/SupportDesk.Api/appsettings.json`
- `backend/SupportDesk.Api/appsettings.Development.json`
- `backend/SupportDesk.Infrastructure/Persistence/DesignTimeSupportDeskDbContextFactory.cs`

This uses Windows authentication (`Trusted_Connection=True`). No SQL password is stored in the repository.

### Adjusting for another instance / LocalDB

Change `ConnectionStrings:SupportDesk` for your machine. Examples:

```text
# Named instance
Server=localhost\SQLEXPRESS;Database=SupportDesk;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true

# LocalDB (if installed)
Server=(localdb)\mssqllocaldb;Database=SupportDesk;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

If you change the backend URL/port, update the Angular proxy as well (see Troubleshooting).

---

## EF Core / Tooling

The repository pins a **local** EF CLI tool:

- **dotnet-ef 9.0.19** in `backend/.config/dotnet-tools.json`

Why:

- the solution uses EF Core **9**
- a globally installed EF 10 CLI should not be used for this repo
- the local tool manifest is authoritative

From the `backend/` directory:

```bash
cd backend
dotnet tool restore
```

Use the restored tool via:

```bash
dotnet ef ...
```

(or `dotnet tool run dotnet-ef ...` if needed)

---

## Database Migration

Current migration:

- **`InitialCreate`** (`20260902214430_InitialCreate`)

Apply manually (from `backend/`):

```bash
cd backend
dotnet tool restore
dotnet ef database update --project SupportDesk.Infrastructure --startup-project SupportDesk.Api
```

In **Development**, `dotnet run` also applies pending migrations automatically (see below). Manual update is useful for first-time setup or troubleshooting.

---

## Seed Data

When `ASPNETCORE_ENVIRONMENT=Development` (the default launch profile), API startup:

1. applies EF migrations (`MigrateAsync`)
2. runs `SupportDeskSeedData.EnsureSeededAsync`

This does **not** wipe or recreate the database.

Seed contents:

| Item | Detail |
|------|--------|
| Agents | **5** (4 active, **1 inactive**) |
| Tickets | **20** (`TCK-2026-0001` … `TCK-2026-0020`) |
| Mix | multiple priorities and statuses; assigned and unassigned; overdue examples; comments |
| Reference counter | year **2026**, `LastValue = 20` |

Next created ticket in 2026 should receive **`TCK-2026-0021`**.

Seed behavior:

- deterministic fixed IDs and timestamps
- idempotent when the expected seed is already complete
- **partial** / unexpected data is **detected and fails startup** rather than silently skipping or wiping
- the application does **not** automatically destroy existing customer/dev data

---

## Running the Backend

```bash
dotnet restore backend/SupportDesk.sln
dotnet run --project backend/SupportDesk.Api
```

Launch profile `http` listens on:

```text
http://localhost:5264
```

(`backend/SupportDesk.Api/Properties/launchSettings.json`)

Development startup automatically migrates and seeds.

---

## Running the Frontend

```bash
cd frontend/support-desk
npm install
npx ng serve
```

App URL:

```text
http://localhost:4200
```

Development proxy (`frontend/support-desk/proxy.conf.json`, wired in `angular.json`):

```text
/api  →  http://localhost:5264
```

So the browser talks to the Angular origin only; **no development CORS setup is required** as long as the API is on port **5264**.

---

## Tests

### Backend

```bash
dotnet build backend/SupportDesk.sln
dotnet test backend/SupportDesk.sln
```

Backend tests use SQL Server (temporary databases on `localhost`). Verified result: **77 passed**.

### Frontend

```bash
cd frontend/support-desk
npx ng test --watch=false
npx ng build
```

Verified result: **10 passed** (Karma / ChromeHeadless).

There is **no CI pipeline** in this repository.

---

## API Overview

### Tickets

| Method | Path |
|--------|------|
| GET | `/api/tickets` |
| GET | `/api/tickets/{id}` |
| POST | `/api/tickets` |
| PUT | `/api/tickets/{id}` |
| DELETE | `/api/tickets/{id}` |
| PUT | `/api/tickets/{id}/assignee` |
| DELETE | `/api/tickets/{id}/assignee` |
| POST | `/api/tickets/{id}/status` |
| POST | `/api/tickets/{id}/comments` |

List query parameters include: `page`, `pageSize`, `search`, `status`, `priority`, `assignedAgentId`, `overdueOnly`.

### Agents

| Method | Path |
|--------|------|
| GET | `/api/agents` |
| GET | `/api/agents/{id}` |

**Why a dedicated status endpoint?** Status change is a workflow command with side effects (timestamps, assignment checks, transition validation). Generic `PUT` update does **not** accept `Status` or other server-controlled fields (`Reference`, dates, assignee, and so on).

---

## Business Rules

### Due date

| Priority | Offset from **CreatedDate** |
|----------|-----------------------------|
| Critical | +4 hours |
| High | +1 day |
| Normal | +3 days |
| Low | +7 days |

- Due date is always calculated from the ticket’s original **CreatedDate**.
- Changing priority while the ticket is **open** recalculates from that same CreatedDate.
- Clients never supply DueDate.

### Status transitions

Allowed only:

- New → InProgress  
- InProgress → Resolved  
- Resolved → Closed  
- Resolved → InProgress  

Everything else is rejected.

### Resolve requirement

**InProgress → Resolved** requires:

- an assigned agent
- that agent must be **active**

### Assignment

- Inactive agents cannot be **newly** assigned.
- If an agent later becomes inactive, existing assignments remain (no automatic unassignment).

### Closed tickets

Closed is terminal and immutable:

- no field edits
- no status changes
- no assign / unassign
- no comments
- no delete

### Timestamps

- `CreatedDate` / `LastModifiedDate` — server-generated (UTC `DateTimeOffset`)
- `ResolvedDate` — set on resolve
- `ClosedDate` — set on close
- Reopen (Resolved → InProgress) clears `ResolvedDate`
- `ClosedDate` is never cleared; Closed cannot reopen

### Overdue

A ticket is overdue when:

- current UTC time is **after** DueDate (`utcNow > DueDate`), and  
- Status is **New** or **InProgress**

At exactly DueDate, the ticket is **not** overdue. `IsOverdue` is derived in responses; it is **not** a database column.

---

## Where Business Rules Live

| Rule | Location |
|------|----------|
| Status transition matrix | `TicketTransitionPolicy` (Domain) |
| Active agent required to **resolve** | `TicketService` (Application) — contextual DB check |
| Assign inactive agent | `TicketAssignmentPolicy` + `TicketService.AssignAsync` |
| Due date offsets / recalculation | `DueDateCalculator` + `TicketService` create/update |
| Closed / open mutability | `TicketMutability` + application service guards |
| Overdue derivation | `OverdueEvaluator` (+ list query projection) |
| System timestamps | Application services + `IClock` |
| Reference allocation | Infrastructure generator inside the create transaction |

**Why this split**

- Domain policies stay pure and unit-testable.
- Application loads contextual data and orchestrates persistence.
- Controllers stay thin (HTTP + DTO mapping).
- Angular only mirrors server state for UX (`allowedTransitions`, capability flags).

---

## Database Design

Main tables / entities:

- **Agent** — Id, FullName, unique Email, Department, Active  
- **Ticket** — business fields, optional `AssignedAgentId`, system dates, DueDate, Reference  
- **Comment** — belongs to Ticket; AuthorName, Body, CreatedDate  
- **TicketReferenceCounter** — per-year sequence (`Year`, `LastValue`)

Also:

- unique `Agent.Email`
- unique `Ticket.Reference`
- nullable `AssignedAgentId`
- Ticket → Comments: **cascade** delete
- Agent → Tickets: **restrict** delete
- dates stored as `datetimeoffset`
- enums stored as **int**
- **no** persisted `IsOverdue` column

---

## Reference Generation

Format:

```text
TCK-YYYY-NNNN
```

Example: `TCK-2026-0001`

Behavior:

- year comes from the UTC creation timestamp
- sequence comes from `TicketReferenceCounters` (not `MAX(Reference)+1`)
- allocation uses SQL Server row locking (`UPDLOCK` / `ROWLOCK` / `HOLDLOCK`) inside a transaction
- ticket insert and counter update share the same transaction on create
- unique DB constraint on `Reference` prevents duplicates
- values are not reused

---

## Validation / Error Handling

ASP.NET Core model validation (`DataAnnotations`) plus application `AppResult` / ProblemDetails mapping.

Machine-readable codes include:

| Code | Typical meaning |
|------|-----------------|
| `VALIDATION_ERROR` | Invalid request shape / field rules |
| `TICKET_NOT_FOUND` / `AGENT_NOT_FOUND` | Missing resource |
| `AGENT_INACTIVE` | Assign or resolve with inactive agent |
| `ASSIGNMENT_REQUIRED` | Resolve without assignee |
| `INVALID_STATUS_TRANSITION` | Illegal status change (includes `currentStatus`, `requestedStatus`, `reason`) |
| `TICKET_NOT_EDITABLE` | Field update when not open (for example Resolved) |
| `TICKET_CLOSED` | Mutation attempted on Closed |

HTTP categories:

| Status | Use |
|--------|-----|
| **400** | Validation |
| **404** | Not found |
| **409** | Business conflict |
| **500** | Unexpected (no stack traces / SQL leaked to clients) |

---

## Angular Design

- Standalone **Angular 20** app
- Feature folders under `src/app`
- `TicketService` / `AgentService` own HTTP
- List filtering, search, overdue-only, and pagination are **server-side**
- Search uses `debounceTime` + `switchMap` to cancel stale requests
- Detail UI is capability-driven (`canEditFields`, `canAssign`, …, `allowedTransitions`)
- **Resolved** locks ordinary fields but still allows workflow actions; **Closed** is fully immutable in the UI
- The API remains authoritative for every mutation

No NgRx or other global state library.

---

## Assumptions and Assignment Ambiguities

These are **documented interpretations** where the assignment text was incomplete or ambiguous:

1. **“Cannot move to … unless an active agent is assigned”**  
   The target status was missing in the brief. **Chosen:** **InProgress → Resolved** requires an active assigned agent.

2. **Overdue wording**  
   One status appeared missing. **Chosen:** only **New** and **InProgress** can be overdue (Resolved/Closed excluded).

3. **“Open” for priority recalculation**  
   **Chosen:** Open = **New** or **InProgress**.

4. **Priority after Resolved**  
   Resolved tickets cannot edit ordinary fields, including Priority (`TICKET_NOT_EDITABLE`).

5. **Reopening**  
   Resolved → InProgress clears `ResolvedDate`.

6. **Closed deletion**  
   Closed tickets cannot be deleted.

7. **Inactive agent**  
   Existing assignments remain; no automatic unassignment.

8. **Create-time assignment**  
   New tickets start unassigned (`Status = New`).

9. **Time**  
   Application timestamps use UTC `DateTimeOffset` via `IClock`.

---

## Key Design Decisions

| Decision | Reason |
|----------|--------|
| Dedicated `POST .../status` | Workflow command with side effects; avoids overposting Status on generic update |
| DTOs on the wire | Prevent overposting; keep EF entities out of the HTTP contract |
| No generic repository | `DbContext` already provides unit-of-work / change tracking for this size of app |
| Application layer inside Api | Small assignment; a fifth project would be ceremony |
| Derived overdue | Avoids stale stored flags as time passes |
| Local reference counter | Concurrency-safe uniqueness without `MAX()+1` |

---

## Known Limitations

- No authentication / authorization (not required by the assignment)
- Concurrent non-reference ticket updates use last-write-wins (no `RowVersion`)
- Development migrate/seed is environment-scoped (Development only)
- No CI pipeline in the repository
- Requires a reachable SQL Server instance
- Frontend automated tests are lighter than the backend suite (10 vs 77)

---

## What I Would Improve With More Time

- Optimistic concurrency (`RowVersion`) on tickets
- Richer Angular component / page tests (list debounce, real detail page)
- Broader API integration coverage (pagination edges, unassign/delete smoke)
- Authentication / authorization if this became a real internal product
- Production database deployment story (migrate outside process startup)
- Status-change audit / history
- Search/indexing tuning if the ticket volume grew large

---

## Approximate Time Spent

**Approximately 5–7 hours** across one development session (evening into early morning), covering domain/persistence/API implementation, Angular UI, automated tests, browser verification, GUID hardening, and this documentation.

This is an **estimate** based on Git commit timestamps (roughly ~2 hours of commit span) plus uncommitted verification/hardening/documentation work. It is not a stopwatch measurement.

---

## Troubleshooting

### SQL Server connection

Edit `ConnectionStrings:SupportDesk` in `appsettings.Development.json` (or user secrets / environment) for your instance. Confirm SQL Server is running and Windows auth can create database `SupportDesk`.

### Port mismatch

Angular proxies `/api` to `http://localhost:5264`. If the API listens elsewhere, update `frontend/support-desk/proxy.conf.json` to match.

### Migrations

From `backend/`:

```bash
dotnet tool restore
dotnet ef database update --project SupportDesk.Infrastructure --startup-project SupportDesk.Api
```

### Existing partial database

Development seed **fails fast** if the database looks partially seeded or contains unexpected data. It does **not** wipe data. Use a fresh database name, or restore/clean the DB intentionally if you need a clean seed—do not treat destructive reset as the normal setup path.

---

## Final Submission Notes

Before submission:

1. `dotnet test backend/SupportDesk.sln`
2. `cd frontend/support-desk && npx ng test --watch=false`
3. `dotnet build backend/SupportDesk.sln`
4. `npx ng build`
5. Confirm a clean Git status
6. Confirm `bin/`, `obj/`, `node_modules/`, `dist/`, `.angular/` remain ignored
7. Confirm no secrets or passwords are committed

Happy reviewing.
