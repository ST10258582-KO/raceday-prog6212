# RaceDay

![CI](https://github.com/ST10258582-KO/raceday-prog6212/actions/workflows/ci.yml/badge.svg)

An event management application designed to support South Africa's road running, walking, and cycling communities.
Two main groups of users interact with the platform: event organisers and participants.

## System Description

RaceDay is a full-stack web-based event management system for South African road events. Organisers can create events, define race categories, capture participant results, and monitor registrations. Participants can register for an account, browse upcoming events, enrol into specific race categories, and review their personal performance history.

This is a three-part POE project:
- **Part 1** – System planning: ERD, API endpoint plan, SQL database schema
- **Part 2** – RESTful API in ASP.NET Core with JWT auth, EF Core, Swagger, unit tests, and CI/CD *(this part)*
- **Part 3** – ASP.NET MVC frontend with Azure Blob Storage and Docker

## User Roles

| Role | Capabilities |
|------|-------------|
| **Organiser** | Create/update/delete events and categories; capture and update participant results; view all enrolments for their events. |
| **Participant** | Register and log in; browse events; enrol in event categories; view own enrolments and results history. |

Role-based access is enforced at the JWT level. All protected endpoints return `401 Unauthorized` if no token is provided, and `403 Forbidden` if the authenticated user's role is incorrect.

## Part 2 — API Setup Instructions

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- SQL Server 2019+ (or SQL Server Express / LocalDB)
- Optional: [Postman](https://www.postman.com/) or the built-in Swagger UI

### 1. Clone and configure

```bash
git clone https://github.com/ST10258582-KO/raceday-prog6212.git
cd raceday-prog6212
```

Edit `src/RaceDay.API/appsettings.json` and set your SQL Server connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "RaceDaySuperSecretKey2026_MustBe32CharsMin!",
    "Issuer": "RaceDayAPI",
    "Audience": "RaceDayClients"
  }
}
```

### 2. Create the database

Run the Part 1 SQL script against your SQL Server instance:

```sql
-- In SSMS or sqlcmd, run:
docs/raceday_schema.sql
```

This creates the `RaceDay` database with all 6 tables and sample seed data.

### 3. Run the API

```bash
cd src/RaceDay.API
dotnet run
```

The Swagger UI will be available at `http://localhost:5000` (or the port shown in the console).

### 4. Run unit tests

```bash
cd src/RaceDay.Tests
dotnet test
```

Tests run against an in-memory database — no SQL Server required for testing.

## API Endpoints

The full 26-endpoint specification is in `docs/api-endpoint-plan.md`. The Swagger UI documents all endpoints with descriptions, request/response schemas, and JWT authentication support.

Key areas covered:
- `POST /api/auth/register` — Register as Organiser or Participant
- `POST /api/auth/login` — Login and receive JWT token
- `GET/POST/PUT/DELETE /api/events` — Event management
- `GET/POST/PUT/DELETE /api/events/{id}/categories` — Category management
- `POST/GET/DELETE /api/enrolments` — Participant enrolments
- `POST/PUT /api/results` + `GET /api/results/my` — Results management
- `GET/POST/PUT /api/events/{id}/route` — Route/map management

## Repository Structure

```
/
├── src/
│   ├── RaceDay.API/                  # ASP.NET Core Web API (Part 2)
│   │   ├── Controllers/              # 6 controllers — Auth, Users, Events, Categories, Enrolments, Results, Routes
│   │   ├── Data/                     # RaceDayContext (EF Core Code-First)
│   │   ├── DTOs/                     # Request/response data transfer objects
│   │   ├── Models/                   # 6 entity models matching the ERD
│   │   └── Program.cs                # App startup — JWT, Swagger, EF Core, auth
│   └── RaceDay.Tests/                # xUnit integration tests (19 tests)
├── docs/
│   ├── ERD.png                       # Entity Relationship Diagram
│   ├── api-endpoint-plan.md          # Full 26-endpoint API spec
│   ├── raceday_schema.sql            # SQL Server schema + seed data
│   └── RaceDay_Part1_ST10258582.docx # Part 1 submission document
└── .github/
    └── workflows/
        ├── validate-docs.yml         # Part 1 CI — validates /docs structure
        └── ci.yml                    # Part 2 CI — build + run all tests
```

## CI/CD

GitHub Actions is configured to build the solution and run all 19 unit tests on every push.

**Workflow:** `.github/workflows/ci.yml`

Steps:
1. Checkout repository
2. Setup .NET 9
3. Restore dependencies
4. Build in Release mode
5. Run all unit tests (in-memory database — no SQL Server needed in CI)

### CI/CD Build Screenshot

> *(Screenshot to be added after first successful GitHub Actions run)*

## Video Presentation (Part 2)

> **YouTube Link:** *(To be added before submission)*

The Part 2 video demonstrates:
- The running API in Swagger UI
- Registration and login with both roles
- Organiser creating events and categories
- Participant enrolling in an event
- Role-based access enforcement (403 errors)
- Unit test execution and results
- Code structure walkthrough

---

*Kent Orchard — ST10258582 — PROG6212 POE*
