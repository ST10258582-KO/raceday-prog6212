# RaceDay – Data Dictionary

This document describes every column in each of the six database tables that make up the RaceDay schema. It serves as the authoritative reference for field names, data types, nullability, constraints, and purpose.

---

## Users

Stores all system accounts. Both Organisers and Participants are held in this single table and differentiated by the `Role` column.

| Column Name  | Data Type       | Nullable | Constraints                                      | Description                                                                 |
|--------------|-----------------|----------|--------------------------------------------------|-----------------------------------------------------------------------------|
| UserID       | INT             | No       | PK, IDENTITY(1,1)                                | Auto-generated surrogate primary key that uniquely identifies each user.    |
| FullName     | NVARCHAR(100)   | No       | NOT NULL                                         | The user's full display name as provided at registration.                   |
| Email        | NVARCHAR(150)   | No       | NOT NULL, UNIQUE (UQ_Users_Email)                | The user's email address. Used as the login identifier; must be unique.     |
| PasswordHash | NVARCHAR(256)   | No       | NOT NULL                                         | BCrypt-hashed password. Plain-text passwords are never stored.              |
| Role         | NVARCHAR(20)    | No       | NOT NULL, CHECK (CK_Users_Role): 'Organiser' or 'Participant' | Determines the user's permission level within the application. |
| PhoneNumber  | NVARCHAR(20)    | Yes      | NULL                                             | Optional contact phone number.                                              |
| DateOfBirth  | DATE            | Yes      | NULL                                             | Optional date of birth, used for age-group category eligibility.            |
| CreatedAt    | DATETIME2       | No       | NOT NULL, DEFAULT GETDATE() (DF_Users_CreatedAt) | Timestamp automatically set when the user record is first inserted.         |
| IsActive     | BIT             | No       | NOT NULL, DEFAULT 1 (DF_Users_IsActive)          | Soft-delete flag. 1 = active account; 0 = deactivated.                     |

---

## Events

Represents a road running, walking, or cycling event created by an Organiser.

| Column Name | Data Type       | Nullable | Constraints                                                                                         | Description                                                                          |
|-------------|-----------------|----------|-----------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------|
| EventID     | INT             | No       | PK, IDENTITY(1,1)                                                                                   | Auto-generated surrogate primary key for the event.                                  |
| OrganiserID | INT             | No       | NOT NULL, FK → Users(UserID) (FK_Events_Organiser)                                                  | References the Users table; identifies which Organiser created the event.            |
| Name        | NVARCHAR(150)   | No       | NOT NULL                                                                                            | The public-facing title of the event (e.g. "Cape Town Sunrise 10K").                |
| Description | NVARCHAR(MAX)   | Yes      | NULL                                                                                                | Optional long-form description providing details about the event.                    |
| EventDate   | DATE            | No       | NOT NULL                                                                                            | The scheduled date on which the event takes place.                                   |
| Location    | NVARCHAR(200)   | No       | NOT NULL                                                                                            | Street-level address or venue name for the event start.                              |
| City        | NVARCHAR(100)   | No       | NOT NULL                                                                                            | City in which the event is held. Used for browsing and filtering.                    |
| Province    | NVARCHAR(100)   | No       | NOT NULL                                                                                            | Province in which the event is held (e.g. "Western Cape").                           |
| Status      | NVARCHAR(20)    | No       | NOT NULL, DEFAULT 'Upcoming', CHECK (CK_Events_Status): 'Upcoming','Open','Closed','Completed','Cancelled' | Lifecycle state of the event. Controls whether enrolments are accepted. |
| ImageURL    | NVARCHAR(500)   | Yes      | NULL                                                                                                | Optional URL to a banner or promotional image for the event.                         |
| CreatedAt   | DATETIME2       | No       | NOT NULL, DEFAULT GETDATE() (DF_Events_CreatedAt)                                                   | Timestamp automatically set when the event record is created.                        |

---

## Categories

Defines a specific race or activity type within an event. An event may have multiple categories (e.g. 5K Run, 10K Run, Half Marathon).

| Column Name     | Data Type      | Nullable | Constraints                                                                                              | Description                                                                         |
|-----------------|----------------|----------|----------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|
| CategoryID      | INT            | No       | PK, IDENTITY(1,1)                                                                                        | Auto-generated surrogate primary key for the category.                              |
| EventID         | INT            | No       | NOT NULL, FK → Events(EventID) ON DELETE CASCADE (FK_Categories_Event)                                   | References the parent event. Cascades delete so removing an event removes its categories. |
| Name            | NVARCHAR(100)  | No       | NOT NULL                                                                                                 | Display name of the category (e.g. "Half Marathon", "30K Road Cycle").             |
| Distance        | DECIMAL(6,2)   | No       | NOT NULL, CHECK > 0 (CK_Categories_Distance)                                                             | Race distance in the specified unit. Must be a positive value.                      |
| DistanceUnit    | NVARCHAR(10)   | No       | NOT NULL, DEFAULT 'km', CHECK (CK_Categories_Unit): 'km' or 'm'                                          | Unit of measurement for the distance column.                                        |
| EntryFee        | DECIMAL(10,2)  | No       | NOT NULL, DEFAULT 0.00, CHECK >= 0 (CK_Categories_Fee)                                                   | Entry fee in South African Rand. Zero indicates a free category.                    |
| MaxParticipants | INT            | Yes      | NULL                                                                                                     | Optional cap on the number of participants allowed in this category.                |
| EventType       | NVARCHAR(20)   | No       | NOT NULL, CHECK (CK_Categories_Type): 'Running', 'Walking', or 'Cycling'                                 | Activity type for this category. Determines timing rules and result tracking.       |
| CreatedAt       | DATETIME2      | No       | NOT NULL, DEFAULT GETDATE() (DF_Categories_CreatedAt)                                                    | Timestamp automatically set when the category is added.                             |

---

## EventRoutes

Stores optional route and map information for an event. The one-to-one unique constraint on EventID ensures each event has at most one route record.

| Column Name      | Data Type      | Nullable | Constraints                                                                        | Description                                                                              |
|------------------|----------------|----------|------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| RouteID          | INT            | No       | PK, IDENTITY(1,1)                                                                  | Auto-generated surrogate primary key for the route record.                               |
| EventID          | INT            | No       | NOT NULL, FK → Events(EventID) ON DELETE CASCADE (FK_Routes_Event), UNIQUE (UQ_Routes_EventID) | References the parent event. The unique constraint enforces one route per event. |
| RouteDescription | NVARCHAR(MAX)  | Yes      | NULL                                                                               | Plain-text description of the course, including landmarks and turn-by-turn guidance.     |
| MapURL           | NVARCHAR(500)  | Yes      | NULL                                                                               | URL to an external interactive map (e.g. Google Maps or Strava route link).              |
| GPXData          | NVARCHAR(MAX)  | Yes      | NULL                                                                               | Raw GPX (GPS Exchange Format) XML data for the route, enabling download to GPS devices.  |
| ElevationGain    | DECIMAL(8,2)   | Yes      | NULL                                                                               | Total elevation gain in metres for the route.                                            |
| CreatedAt        | DATETIME2      | No       | NOT NULL, DEFAULT GETDATE() (DF_Routes_CreatedAt)                                  | Timestamp automatically set when the route record is inserted.                           |

---

## Enrolments

Records a Participant's registration for a specific Category within an event. The composite unique constraint prevents the same participant from enrolling in the same category twice.

| Column Name   | Data Type     | Nullable | Constraints                                                                                             | Description                                                                              |
|---------------|---------------|----------|---------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| EnrolmentID   | INT           | No       | PK, IDENTITY(1,1)                                                                                       | Auto-generated surrogate primary key for the enrolment record.                           |
| ParticipantID | INT           | No       | NOT NULL, FK → Users(UserID) (FK_Enrolments_Participant)                                                | References the Participant who enrolled. Non-cascading to preserve enrolment history.    |
| CategoryID    | INT           | No       | NOT NULL, FK → Categories(CategoryID) (FK_Enrolments_Category)                                         | References the Category the participant enrolled in.                                     |
| EnrolmentDate | DATETIME2     | No       | NOT NULL, DEFAULT GETDATE() (DF_Enrolments_Date)                                                        | Timestamp automatically set when the participant registers for the category.             |
| PaymentStatus | NVARCHAR(20)  | No       | NOT NULL, DEFAULT 'Pending', CHECK (CK_Enrolments_PayStatus): 'Pending', 'Paid', or 'Refunded'          | Tracks whether the entry fee has been received, is outstanding, or has been refunded.    |
| BibNumber     | NVARCHAR(20)  | Yes      | NULL                                                                                                    | Race bib number assigned to the participant by the Organiser before race day.            |
| —             | —             | —        | UNIQUE (UQ_Enrolment_ParticipantCategory): (ParticipantID, CategoryID)                                  | Composite unique constraint preventing duplicate enrolments per participant per category.|

---

## Results

Records a participant's race outcome after the event. Linked to the Enrolments table (not directly to Users) to preserve the specific category context. One result per enrolment.

| Column Name | Data Type     | Nullable | Constraints                                                                                   | Description                                                                               |
|-------------|---------------|----------|-----------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------|
| ResultID    | INT           | No       | PK, IDENTITY(1,1)                                                                             | Auto-generated surrogate primary key for the result record.                               |
| EnrolmentID | INT           | No       | NOT NULL, FK → Enrolments(EnrolmentID) (FK_Results_Enrolment), UNIQUE (UQ_Results_Enrolment) | References the specific enrolment this result belongs to. Unique enforces one result per enrolment. |
| FinishTime  | TIME          | Yes      | NULL                                                                                          | Clock time taken to complete the race (e.g. 00:52:34). NULL if the participant did not finish. |
| Position    | INT           | Yes      | NULL, CHECK > 0 (CK_Results_Position)                                                         | Finishing position within the category. NULL if not yet ranked or participant did not finish. |
| Status      | NVARCHAR(20)  | No       | NOT NULL, DEFAULT 'DNS', CHECK (CK_Results_Status): 'Finished', 'DNF', 'DNS', or 'DQ'        | Race outcome code. DNS = Did Not Start, DNF = Did Not Finish, DQ = Disqualified.          |
| Notes       | NVARCHAR(500) | Yes      | NULL                                                                                          | Optional free-text notes added by the Organiser (e.g. reason for disqualification).       |
| RecordedAt  | DATETIME2     | No       | NOT NULL, DEFAULT GETDATE() (DF_Results_RecordedAt)                                           | Timestamp automatically set when the result record is first inserted.                     |
