# RaceDay

An event management application to support the South African road running, walking, and cycling communities.
It includes an organiser portal for managing events, a participant portal for browsing events and entering races, and a personal performance tracking feature for participants.

## System Description

Event organisers can use RaceDay to create events and manage them. Event categories can be defined by organisers to help categorise the race types. Participant results can be captured and updated by organisers. Event organisers also have the capability to see all registrations for their events. Participants can register for an account and then search for upcoming events. Once they find an event that interests them, they can enrol into it. They also have the ability to review past performances.

## User Roles

| Role | Capabilities |
|------|-------------|
| **Organiser** | Can create, modify and remove events; define race categories; capture and update participant results; view all participants registered for their events. |
| **Participant** | Can create an account; browse upcoming events; enrol in a specific event category; view their own enrolments and personal results history. |

Access control is implemented through role-based authentication at the API layer (Part 2), which is then reflected in the MVC interface (Part 3).

## Repository Structure

```
/
├── docs/
│   ├── ERD.png                   # Entity Relationship Diagram (Section A)
│   ├── ERD.md                    # Mermaid source for the ERD
│   ├── api-endpoint-plan.md      # Full API endpoint specification (Section B)
│   └── raceday_schema.sql        # SQL Server database schema + seed data (Section C)
├── .github/
│   └── workflows/
│       └── validate-docs.yml     # CI/CD workflow – validates /docs structure
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

> **CI/CD Screenshot:** *(Add screenshot of successful green build here after first push)*

## Video Presentation

> **YouTube Link:** *(Add unlisted YouTube link here after recording)*

The video covers:
- Walkthrough of the ERD and the design decisions behind it
- Explanation of the API endpoint plan
- Live demonstration of the SQL script running in SSMS
