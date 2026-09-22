# RaceDay

![CI/CD](https://github.com/ST10258582-KO/raceday-prog6212/actions/workflows/validate-docs.yml/badge.svg)

An event management application designed to support South Africa's road running, walking, and cycling communities.
Two main groups of users interact with the platform: event organisers and participants.

## System Description

Organisers can create events, define race categories, capture participant results, and monitor registrations across all their events. Participants can register for an account, browse upcoming events, enrol into specific race categories, and review their past performance history.

RaceDay follows a three-tier architecture. Part 1 covers the planning layer, which includes the entity relationship diagram, the API endpoint specification, and the SQL Server database schema. Part 2 will implement the back-end API layer, and Part 3 will deliver the front-end interface using ASP.NET MVC.

## User Roles

| Role | Capabilities |
|------|-------------|
| **Organiser** | Can create, modify and remove events; define race categories; capture and update participant results; view all participants registered for their events. |
| **Participant** | Can create an account; browse upcoming events; enrol in a specific event category; view their own enrolments and personal results history. |

Access control is role-based. All users are stored in a single Users table and differentiated by a Role column, enforced through a CHECK constraint at the database level and validated via JWT authentication at the API layer.

## Database Design Decisions

**Single-table user design:** Both Organisers and Participants are stored in the same `Users` table and distinguished by the `Role` column. This simplifies the authentication flow and avoids join complexity when resolving role claims from a JWT token.

**Cascade deletes on child tables:** The `Categories` and `EventRoutes` tables use `ON DELETE CASCADE` on their foreign keys to `Events`. Deleting an event automatically removes all associated categories and its route record, keeping the database consistent without requiring multiple API-layer DELETE calls.

**Composite unique constraint on Enrolments:** The `UQ_Enrolment_ParticipantCategory` constraint spans `(ParticipantID, CategoryID)`, enforcing at the database level that a participant may only enter a given category once. This protects against duplicate enrolments even if the application layer fails to validate the rule.

**Status lifecycle via CHECK constraints:** The `Status` columns on `Events`, `Enrolments`, and `Results` use `CHECK` constraints rather than lookup tables. For small, stable sets of values (five or fewer), this keeps the schema simple and the allowed values visible directly in the `CREATE TABLE` definition without additional joins.

## API Design Principles

**JWT authentication:** All protected API endpoints require a bearer token issued on login. The token encodes the user's ID and role as claims, allowing role checks without additional database queries on each request.

**RESTful conventions:** Resources are named with lowercase plural nouns (`/events`, `/categories`, `/enrolments`). HTTP verbs carry the action intent — GET for reads, POST for creates, PUT for updates, DELETE for removals. Sub-resources are expressed as nested paths (e.g. `/events/{id}/categories`).

**Role enforcement:** Endpoints are categorised as public (no token required), any authenticated user, Organiser-only, or Participant-only. The API validates the role claim in the JWT and returns `403 Forbidden` for mismatched roles. Ownership of resources (e.g. an Organiser modifying their own event) is validated against the stored `OrganiserID`.

**Consistent error codes:** All error responses follow a uniform JSON structure and use standard HTTP status codes. Validation errors return `400`, authentication failures return `401`, permission failures return `403`, missing resources return `404`, and duplicate-resource conflicts return `409`.

## Repository Structure

```
/
├── docs/
│   ├── RaceDay_Part1_ST10258582.docx   # Part 1 submission document
│   ├── ERD.png                         # Entity Relationship Diagram (Section A)
│   ├── ERD.md                          # Mermaid source for the ERD
│   ├── api-endpoint-plan.md            # Full API endpoint specification (Section B)
│   └── raceday_schema.sql              # SQL Server database schema + seed data (Section C)
├── .github/
│   └── workflows/
│       └── validate-docs.yml           # CI/CD workflow – validates /docs structure
└── README.md
```

## Setup Instructions

### Prerequisites
- SQL Server 2019+ or SQL Server Express
- SQL Server Management Studio (SSMS)

### Database Setup

1. Connect to your SQL Server instance using SSMS.
2. Open `docs/raceday_schema.sql`.
3. Run the entire script (F5). It will:
   - Create the `RaceDay` database.
   - Create all 6 tables with their constraints.
   - Insert seed data: 2 Organisers, 2 Participants, 3 Events, 8 Categories, 2 Routes, 4 Enrolments, and 2 Results.
4. A verification query at the end confirms the row count for each table.

### ERD

The ERD source is in `docs/ERD.md` as a Mermaid diagram.
To export as PNG:
1. Open [mermaid.live](https://mermaid.live) and paste the diagram code.
2. Download as PNG and save as `docs/ERD.png`.

## CI/CD

Each time code is pushed to this repository, GitHub Actions validates that all required planning documents exist in the `/docs` folder.

**Workflow:** `.github/workflows/validate-docs.yml`

Checks performed:
- `/docs` folder exists
- ERD image (`ERD.png` or `ERD.pdf`) is present
- API endpoint plan (`api-endpoint-plan.md` or `.pdf`) is present
- At least one `.sql` file is present
- `README.md` exists in the root

### CI/CD Build Screenshot

![Green Build](docs/cicd-screenshot.png)

> Screenshot will be added after first successful push triggers the workflow.

## Video Presentation

> **YouTube Link:** *(To be added before submission)*

The video walkthrough covers:
- ERD design decisions and entity relationships
- API endpoint plan structure and role-based access choices
- Live execution of the SQL script in SSMS
