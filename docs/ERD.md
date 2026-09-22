# RaceDay – Entity Relationship Diagram

> Render this diagram using [mermaid.live](https://mermaid.live) or the VS Code Mermaid extension, then export as PNG and save as `ERD.png` in this folder.

```mermaid
erDiagram
    Users {
        int UserID PK
        nvarchar(100) FullName
        nvarchar(150) Email UK
        nvarchar(256) PasswordHash
        nvarchar(20) Role
        nvarchar(20) PhoneNumber
        date DateOfBirth
        datetime2 CreatedAt
        bit IsActive
    }

    Events {
        int EventID PK
        int OrganiserID FK
        nvarchar(150) Name
        nvarchar(MAX) Description
        date EventDate
        nvarchar(200) Location
        nvarchar(100) City
        nvarchar(100) Province
        nvarchar(20) Status
        nvarchar(500) ImageURL
        datetime2 CreatedAt
    }

    Categories {
        int CategoryID PK
        int EventID FK
        nvarchar(100) Name
        decimal(6_2) Distance
        nvarchar(10) DistanceUnit
        decimal(10_2) EntryFee
        int MaxParticipants
        nvarchar(20) EventType
        datetime2 CreatedAt
    }

    EventRoutes {
        int RouteID PK
        int EventID FK
        nvarchar(MAX) RouteDescription
        nvarchar(500) MapURL
        nvarchar(MAX) GPXData
        decimal(8_2) ElevationGain
        datetime2 CreatedAt
    }

    Enrolments {
        int EnrolmentID PK
        int ParticipantID FK
        int CategoryID FK
        datetime2 EnrolmentDate
        nvarchar(20) PaymentStatus
        nvarchar(20) BibNumber
    }

    Results {
        int ResultID PK
        int EnrolmentID FK
        time FinishTime
        int Position
        nvarchar(20) Status
        nvarchar(500) Notes
        datetime2 RecordedAt
    }

    Users ||--o{ Events : "organises (OrganiserID)"
    Events ||--o{ Categories : "has"
    Events ||--o| EventRoutes : "has"
    Users ||--o{ Enrolments : "makes (ParticipantID)"
    Categories ||--o{ Enrolments : "included in"
    Enrolments ||--o| Results : "produces"
```

## Entity Descriptions

| Entity | Description |
|--------|-------------|
| **Users** | Single table for both Organisers and Participants, distinguished by the `Role` column. |
| **Events** | Road running, walking, or cycling events created by Organisers. |
| **Categories** | Race categories within an event (e.g. 5K Run, 10K Run, 21K Walk). |
| **EventRoutes** | Optional route/map data attached to an event. One-to-one with Events. |
| **Enrolments** | A Participant's entry into a specific Category. Enforces unique per participant per category. |
| **Results** | Finish time and position recorded by an Organiser after the event. One-to-one with an Enrolment. |

## Cardinality Notes

- A **User** (Organiser) can organise **many Events** (one-to-many).
- An **Event** can have **many Categories** (one-to-many).
- An **Event** has **at most one Route** (one-to-one).
- A **User** (Participant) can have **many Enrolments** (one-to-many).
- A **Category** can have **many Enrolments** (one-to-many).
- An **Enrolment** produces **at most one Result** (one-to-one).
