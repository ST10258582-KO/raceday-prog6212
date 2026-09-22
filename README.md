# RaceDay

A full-stack web-based event management system for the South African road running, walking, and cycling community.

## System Description

RaceDay enables event management for road-based sporting events such as park runs, community walks, and cycling tours. Organisers can create and manage events, define race categories, and capture participant results. Participants can browse upcoming events, enter a race, and track their personal performance history.

## User Roles

| Role | Capabilities |
|------|-------------|
| **Organiser** | Create, edit, and delete events; manage event categories; capture and update participant results; view all enrolments for their events. |
| **Participant** | Register an account; browse all upcoming events; enrol in a specific event category; view their own enrolments and personal results history. |

Role-based access is enforced at the API level (Part 2) and reflected in the MVC interface (Part 3).

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

1. Open SSMS and connect to your SQL Server instance.
2. Open `docs/raceday_schema.sql`.
3. Execute the full script (F5). It will:
   - Create the `RaceDay` database.
   - Create all 6 tables with constraints.
   - Insert seed data: 2 Organisers, 2 Participants, 3 Events, 8 Categories, 2 Routes, 4 Enrolments, and 2 Results.
4. The verification query at the end confirms row counts for each table.

### ERD

The ERD source is in `docs/ERD.md` as a Mermaid diagram.
To export as PNG:
1. Open [mermaid.live](https://mermaid.live) and paste the diagram code.
2. Download as PNG and save as `docs/ERD.png`.

## CI/CD

This repository uses GitHub Actions to validate that all required planning documents are present in the `/docs` folder on every push.

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
- Walkthrough of the ERD and design decisions
- Explanation of the API endpoint plan
- Live demonstration of the SQL script running in SSMS
