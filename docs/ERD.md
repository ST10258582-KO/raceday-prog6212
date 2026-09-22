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
| **Users** | Both Organisers and Participants are stored in the same Users table. The role of each user is defined in the `Role` column. |
| **Events** | These represent road running, walking, or cycling races organised by users with the Organiser role. |
| **Categories** | These define race types within an event (for example, 5K Run, 10K Run, 21K Walk). |
| **EventRoutes** | Optional map and route information that may be associated with an event. Each EventRoutes record maps to exactly one Event. |
| **Enrolments** | Records a Participant's registration for a specific Category. A unique constraint ensures no participant can enrol in the same category more than once. |
| **Results** | Records finish times and positions entered by an Organiser after the event. Each Results record is linked to exactly one Enrolment. |

## Cardinality Notes

- A User (Organiser) can create many Events (one-to-many).
- An Event can have many Categories (one-to-many).
- An Event will never have more than one Route (one-to-one).
- A User (Participant) can make many Enrolments (one-to-many).
- A Category can be associated with many Enrolments (one-to-many).
- One Enrolment will produce no more than one Result (one-to-one).
