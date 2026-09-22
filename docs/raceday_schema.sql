-- =============================================================
-- RaceDay - Full Database Schema and Seed Data
-- SQL Server (SSMS compatible)
-- Run this script on a clean SQL Server instance.
-- =============================================================

USE master;
GO

-- Create the database if it does not already exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'RaceDay')
BEGIN
    CREATE DATABASE RaceDay;
END
GO

USE RaceDay;
GO

-- =============================================================
-- DROP EXISTING TABLES (dependency order: children first)
-- =============================================================
IF OBJECT_ID('dbo.Results',     'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments',  'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.EventRoutes', 'U') IS NOT NULL DROP TABLE dbo.EventRoutes;
IF OBJECT_ID('dbo.Categories',  'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events',      'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users',       'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- =============================================================
-- TABLE: Users
-- Users stores all system accounts (Organisers and Participants) in a single table.
-- The Role column differentiates account types and is enforced by a CHECK constraint.
-- A single-table design simplifies authentication and JWT role-claim generation.
-- =============================================================
CREATE TABLE dbo.Users (
    UserID        INT            IDENTITY(1,1)  NOT NULL,  -- Surrogate PK, auto-incremented
    FullName      NVARCHAR(100)                 NOT NULL,  -- Display name
    Email         NVARCHAR(150)                 NOT NULL,  -- Used as login identifier
    PasswordHash  NVARCHAR(256)                 NOT NULL,  -- BCrypt hash; never store plain text
    Role          NVARCHAR(20)                  NOT NULL,  -- 'Organiser' or 'Participant'
    PhoneNumber   NVARCHAR(20)                  NULL,      -- Optional contact number
    DateOfBirth   DATE                          NULL,      -- Optional; used for age-group eligibility
    CreatedAt     DATETIME2                     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE(),  -- Auto-stamped on insert
    IsActive      BIT                           NOT NULL CONSTRAINT DF_Users_IsActive  DEFAULT 1,          -- Soft-delete flag

    -- Primary key and uniqueness constraints
    CONSTRAINT PK_Users        PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_Email  UNIQUE      (Email),       -- Prevents duplicate accounts per email
    CONSTRAINT CK_Users_Role   CHECK       (Role IN ('Organiser', 'Participant'))  -- Restricts to known roles
);
GO

-- =============================================================
-- TABLE: Events
-- Events represents road running, walking, or cycling races organised by Organisers.
-- The Status column follows a defined lifecycle: Upcoming → Open → Closed → Completed.
-- OrganiserID links each event to its owning User; only that user may modify the event.
-- =============================================================
CREATE TABLE dbo.Events (
    EventID      INT            IDENTITY(1,1)  NOT NULL,   -- Surrogate PK
    OrganiserID  INT                           NOT NULL,   -- FK to Users (the creating Organiser)
    Name         NVARCHAR(150)                 NOT NULL,   -- Public event title
    Description  NVARCHAR(MAX)                 NULL,       -- Optional detailed description
    EventDate    DATE                          NOT NULL,   -- Scheduled date of the event
    Location     NVARCHAR(200)                 NOT NULL,   -- Street address or venue name
    City         NVARCHAR(100)                 NOT NULL,   -- City for browsing/filtering
    Province     NVARCHAR(100)                 NOT NULL,   -- Province for browsing/filtering
    Status       NVARCHAR(20)                  NOT NULL CONSTRAINT DF_Events_Status DEFAULT 'Upcoming',  -- Lifecycle state
    ImageURL     NVARCHAR(500)                 NULL,       -- Optional promotional image URL
    CreatedAt    DATETIME2                     NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT GETDATE(), -- Auto-stamped

    -- Primary key, foreign keys, and value constraints
    CONSTRAINT PK_Events           PRIMARY KEY (EventID),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID),
    -- Status must be one of the defined lifecycle values
    CONSTRAINT CK_Events_Status    CHECK       (Status IN ('Upcoming', 'Open', 'Closed', 'Completed', 'Cancelled'))
);
GO

-- =============================================================
-- TABLE: Categories
-- Categories defines the individual race types within an event (e.g. 5K Run, Half Marathon).
-- An event must have at least one category for participants to enrol.
-- CASCADE DELETE ensures categories are removed automatically when their parent event is deleted.
-- =============================================================
CREATE TABLE dbo.Categories (
    CategoryID       INT            IDENTITY(1,1)  NOT NULL,  -- Surrogate PK
    EventID          INT                           NOT NULL,  -- FK to parent Event
    Name             NVARCHAR(100)                 NOT NULL,  -- Category label (e.g. '10K Race')
    Distance         DECIMAL(6, 2)                 NOT NULL,  -- Race distance; must be positive
    DistanceUnit     NVARCHAR(10)                  NOT NULL CONSTRAINT DF_Categories_Unit    DEFAULT 'km',   -- 'km' or 'm'
    EntryFee         DECIMAL(10, 2)                NOT NULL CONSTRAINT DF_Categories_Fee     DEFAULT 0.00,   -- Entry fee in ZAR; 0 = free
    MaxParticipants  INT                           NULL,      -- Optional cap; NULL = unlimited
    EventType        NVARCHAR(20)                  NOT NULL,  -- 'Running', 'Walking', or 'Cycling'
    CreatedAt        DATETIME2                     NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT GETDATE(), -- Auto-stamped

    -- Primary key and foreign key (cascade keeps data consistent on event deletion)
    CONSTRAINT PK_Categories           PRIMARY KEY (CategoryID),
    CONSTRAINT FK_Categories_Event     FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID) ON DELETE CASCADE,
    -- Value range and domain constraints
    CONSTRAINT CK_Categories_Unit      CHECK       (DistanceUnit IN ('km', 'm')),
    CONSTRAINT CK_Categories_Type      CHECK       (EventType IN ('Running', 'Walking', 'Cycling')),
    CONSTRAINT CK_Categories_Distance  CHECK       (Distance > 0),
    CONSTRAINT CK_Categories_Fee       CHECK       (EntryFee >= 0)
);
GO

-- =============================================================
-- TABLE: EventRoutes
-- EventRoutes stores optional map and GPS route data for an event.
-- Kept in a separate table so events without a route do not carry null columns.
-- The UNIQUE constraint on EventID enforces the one-to-one relationship with Events.
-- CASCADE DELETE removes the route automatically when the event is deleted.
-- =============================================================
CREATE TABLE dbo.EventRoutes (
    RouteID           INT            IDENTITY(1,1)  NOT NULL,  -- Surrogate PK
    EventID           INT                           NOT NULL,  -- FK to Events; must be unique (one route per event)
    RouteDescription  NVARCHAR(MAX)                 NULL,      -- Plain-text course description
    MapURL            NVARCHAR(500)                 NULL,      -- Link to interactive map
    GPXData           NVARCHAR(MAX)                 NULL,      -- Raw GPX XML for GPS device download
    ElevationGain     DECIMAL(8, 2)                 NULL,      -- Total ascent in metres
    CreatedAt         DATETIME2                     NOT NULL CONSTRAINT DF_Routes_CreatedAt DEFAULT GETDATE(), -- Auto-stamped

    -- Primary key; FK cascades so route is deleted with the event
    CONSTRAINT PK_EventRoutes       PRIMARY KEY (RouteID),
    CONSTRAINT FK_Routes_Event      FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID) ON DELETE CASCADE,
    -- Unique on EventID enforces one-to-one with Events
    CONSTRAINT UQ_Routes_EventID    UNIQUE      (EventID)
);
GO

-- =============================================================
-- TABLE: Enrolments
-- Enrolments records a Participant's registration for a specific Category.
-- The composite UNIQUE constraint enforces the business rule that a participant
-- may only enter a given category once, enforced at the database level.
-- No CASCADE DELETE on FKs here; enrolment history must be preserved.
-- =============================================================
CREATE TABLE dbo.Enrolments (
    EnrolmentID    INT            IDENTITY(1,1)  NOT NULL,  -- Surrogate PK
    ParticipantID  INT                           NOT NULL,  -- FK to Users (Participant)
    CategoryID     INT                           NOT NULL,  -- FK to Categories
    EnrolmentDate  DATETIME2                     NOT NULL CONSTRAINT DF_Enrolments_Date  DEFAULT GETDATE(),  -- Auto-stamped
    PaymentStatus  NVARCHAR(20)                  NOT NULL CONSTRAINT DF_Enrolments_Pay   DEFAULT 'Pending', -- Payment lifecycle
    BibNumber      NVARCHAR(20)                  NULL,      -- Assigned by Organiser before race day

    -- Primary key and foreign keys (no cascade; history must be preserved)
    CONSTRAINT PK_Enrolments              PRIMARY KEY (EnrolmentID),
    CONSTRAINT FK_Enrolments_Participant  FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Category     FOREIGN KEY (CategoryID)    REFERENCES dbo.Categories(CategoryID),
    -- Composite unique: one enrolment per participant per category
    CONSTRAINT UQ_Enrolment_ParticipantCategory UNIQUE (ParticipantID, CategoryID),
    -- Payment status domain constraint
    CONSTRAINT CK_Enrolments_PayStatus    CHECK       (PaymentStatus IN ('Pending', 'Paid', 'Refunded'))
);
GO

-- =============================================================
-- TABLE: Results
-- Results records finish times and positions entered by an Organiser after the event.
-- Linked to Enrolments (not directly to Users) so each result always carries
-- full context: participant, category, event, and bib number via one join.
-- The UNIQUE constraint on EnrolmentID enforces one-to-one with Enrolments.
-- =============================================================
CREATE TABLE dbo.Results (
    ResultID      INT            IDENTITY(1,1)  NOT NULL,  -- Surrogate PK
    EnrolmentID   INT                           NOT NULL,  -- FK to Enrolments (not Users directly)
    FinishTime    TIME                          NULL,      -- Clock time; NULL if DNS/DNF
    Position      INT                           NULL,      -- Category finishing position; NULL if not ranked
    Status        NVARCHAR(20)                  NOT NULL CONSTRAINT DF_Results_Status DEFAULT 'DNS',  -- Race outcome code
    Notes         NVARCHAR(500)                 NULL,      -- Optional Organiser notes (e.g. DQ reason)
    RecordedAt    DATETIME2                     NOT NULL CONSTRAINT DF_Results_RecordedAt DEFAULT GETDATE(), -- Auto-stamped

    -- Primary key and FK; UNIQUE enforces one result per enrolment
    CONSTRAINT PK_Results            PRIMARY KEY (ResultID),
    CONSTRAINT FK_Results_Enrolment  FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID),
    CONSTRAINT UQ_Results_Enrolment  UNIQUE      (EnrolmentID),
    -- Status must be one of the four recognised race outcome codes
    CONSTRAINT CK_Results_Status     CHECK       (Status IN ('Finished', 'DNF', 'DNS', 'DQ')),
    -- Position must be a positive integer if provided
    CONSTRAINT CK_Results_Position   CHECK       (Position IS NULL OR Position > 0)
);
GO

-- =============================================================
-- SEED DATA
-- =============================================================

-- ----------------------------
-- 2 Organisers
-- ----------------------------
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, PhoneNumber, DateOfBirth)
VALUES
    (
        'Thabo Nkosi',
        'thabo.nkosi@raceday.co.za',
        'AQAAAAIAAYagAAAAEHashed1ExampleOrganiser1==',  -- BCrypt placeholder
        'Organiser',
        '0821234567',
        '1985-03-15'
    ),
    (
        'Sandi van der Merwe',
        'sandi.vandermerwe@raceday.co.za',
        'AQAAAAIAAYagAAAAEHashed2ExampleOrganiser2==',
        'Organiser',
        '0719876543',
        '1979-07-22'
    );

-- ----------------------------
-- 2 Participants
-- ----------------------------
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, PhoneNumber, DateOfBirth)
VALUES
    (
        'Lerato Dlamini',
        'lerato.dlamini@gmail.com',
        'AQAAAAIAAYagAAAAEHashed3ExampleParticipant1==',
        'Participant',
        '0833456789',
        '1995-11-08'
    ),
    (
        'Michael Botha',
        'michael.botha@gmail.com',
        'AQAAAAIAAYagAAAAEHashed4ExampleParticipant2==',
        'Participant',
        '0764567890',
        '1988-05-30'
    );
-- UserID 1 = Thabo (Organiser), 2 = Sandi (Organiser)
-- UserID 3 = Lerato (Participant), 4 = Michael (Participant)

-- ----------------------------
-- 3 Events
-- ----------------------------
INSERT INTO dbo.Events (OrganiserID, Name, Description, EventDate, Location, City, Province, Status)
VALUES
    (
        1,
        'Cape Town Sunrise 10K',
        'A scenic morning run along the Atlantic Seaboard. Suitable for all fitness levels with 5K and 10K categories available.',
        '2026-11-01',
        'Sea Point Promenade, Beach Road',
        'Cape Town',
        'Western Cape',
        'Open'
    ),
    (
        1,
        'Sandton City Cycle Tour',
        'Urban cycling event through Sandton CBD and surrounds. Road and mountain bike categories for intermediate and advanced riders.',
        '2026-12-06',
        'Sandton Convention Centre, Maude Street',
        'Sandton',
        'Gauteng',
        'Upcoming'
    ),
    (
        2,
        'Durban Beachfront Marathon',
        'Full and half marathon along the iconic Golden Mile beachfront. A flat, fast course attracting runners from across KwaZulu-Natal.',
        '2027-01-17',
        'Moses Mabhida Stadium, 44 Isaiah Ntshangase Rd',
        'Durban',
        'KwaZulu-Natal',
        'Upcoming'
    );
-- EventID 1 = Cape Town Sunrise 10K, 2 = Sandton Cycle Tour, 3 = Durban Marathon

-- ----------------------------
-- Categories per Event
-- ----------------------------

-- Event 1: Cape Town Sunrise 10K
INSERT INTO dbo.Categories (EventID, Name, Distance, EntryFee, MaxParticipants, EventType)
VALUES
    (1, '5K Fun Run',   5.00,  150.00, 500,  'Running'),
    (1, '10K Race',    10.00,  250.00, 1000, 'Running'),
    (1, '5K Walk',      5.00,  100.00, 300,  'Walking');

-- Event 2: Sandton City Cycle Tour
INSERT INTO dbo.Categories (EventID, Name, Distance, EntryFee, MaxParticipants, EventType)
VALUES
    (2, '30K Road Cycle', 30.00, 350.00, 400, 'Cycling'),
    (2, '60K Road Cycle', 60.00, 500.00, 200, 'Cycling');

-- Event 3: Durban Beachfront Marathon
INSERT INTO dbo.Categories (EventID, Name, Distance, EntryFee, MaxParticipants, EventType)
VALUES
    (3, '10K Walk',      10.00,  150.00,  800, 'Walking'),
    (3, 'Half Marathon', 21.10,  350.00, 2000, 'Running'),
    (3, 'Full Marathon', 42.20,  500.00, 1000, 'Running');
-- CategoryID 1=5KRun, 2=10KRace, 3=5KWalk, 4=30KCycle, 5=60KCycle, 6=10KWalk, 7=Half, 8=Full

-- ----------------------------
-- Event Routes
-- ----------------------------
INSERT INTO dbo.EventRoutes (EventID, RouteDescription, MapURL, ElevationGain)
VALUES
    (
        1,
        'Start at Sea Point Pool, proceed north along Beach Road promenade to Green Point Lighthouse and return. Mostly flat with minor inclines near the Waterfront.',
        'https://maps.raceday.co.za/events/1/route',
        25.50
    ),
    (
        3,
        'Start at Moses Mabhida Stadium, head north along the beachfront promenade past Bay of Plenty to Blue Lagoon, then return on the same route. Half marathon turns at North Beach.',
        'https://maps.raceday.co.za/events/3/route',
        45.00
    );

-- ----------------------------
-- Event Routes (cont.)
-- ----------------------------
INSERT INTO dbo.EventRoutes (EventID, RouteDescription, MapURL, ElevationGain)
VALUES (
    2,
    'Start at Sandton Convention Centre, proceed north along Rivonia Road to Fourways, loop through Magaliessig and return via William Nicol Drive. Mix of flat sections and moderate climbs through the northern suburbs.',
    'https://maps.raceday.co.za/events/2/route',
    310.00
);

-- ----------------------------
-- Sample Enrolments
-- ----------------------------
INSERT INTO dbo.Enrolments (ParticipantID, CategoryID, PaymentStatus, BibNumber)
VALUES
    (3, 2, 'Paid',    'CPT-001'),   -- Lerato → 10K Race (Event 1)
    (4, 1, 'Paid',    'CPT-100'),   -- Michael → 5K Fun Run (Event 1)
    (3, 7, 'Pending', NULL),        -- Lerato → Half Marathon (Event 3)
    (4, 8, 'Paid',    'DBN-042');   -- Michael → Full Marathon (Event 3)
-- EnrolmentID 1=Lerato/10K, 2=Michael/5K, 3=Lerato/Half, 4=Michael/Full

-- ----------------------------
-- Sample Results
-- ----------------------------
INSERT INTO dbo.Results (EnrolmentID, FinishTime, Position, Status)
VALUES
    (1, '00:52:34', 15, 'Finished'),   -- Lerato finished 10K Race
    (2, '00:28:11', 42, 'Finished');   -- Michael finished 5K Fun Run

GO

-- =============================================================
-- RECOMMENDED INDEXES FOR COMMON QUERY PATTERNS
-- =============================================================

-- Fast lookup of events by organiser
CREATE NONCLUSTERED INDEX IX_Events_OrganiserID
    ON dbo.Events(OrganiserID);

-- Fast lookup of categories by event
CREATE NONCLUSTERED INDEX IX_Categories_EventID
    ON dbo.Categories(EventID);

-- Fast lookup of enrolments by participant
CREATE NONCLUSTERED INDEX IX_Enrolments_ParticipantID
    ON dbo.Enrolments(ParticipantID);

-- Fast lookup of enrolments by category
CREATE NONCLUSTERED INDEX IX_Enrolments_CategoryID
    ON dbo.Enrolments(CategoryID);

-- Fast lookup of upcoming events by date and status
CREATE NONCLUSTERED INDEX IX_Events_StatusDate
    ON dbo.Events(Status, EventDate);
GO

-- =============================================================
-- VIEWS
-- =============================================================

-- View: Summary of all events with organiser name and category count
CREATE OR ALTER VIEW dbo.vw_EventSummary AS
SELECT
    e.EventID,
    e.Name          AS EventName,
    e.EventDate,
    e.City,
    e.Province,
    e.Status,
    u.FullName       AS OrganiserName,
    COUNT(c.CategoryID) AS CategoryCount
FROM dbo.Events e
JOIN  dbo.Users      u ON u.UserID    = e.OrganiserID
LEFT JOIN dbo.Categories c ON c.EventID = e.EventID
GROUP BY e.EventID, e.Name, e.EventDate, e.City, e.Province, e.Status, u.FullName;
GO

-- View: Participant enrolment and result history
CREATE OR ALTER VIEW dbo.vw_ParticipantHistory AS
SELECT
    u.UserID         AS ParticipantID,
    u.FullName       AS ParticipantName,
    ev.Name          AS EventName,
    ev.EventDate,
    cat.Name         AS CategoryName,
    cat.Distance,
    cat.DistanceUnit,
    en.PaymentStatus,
    en.BibNumber,
    r.FinishTime,
    r.Position,
    r.Status         AS ResultStatus
FROM dbo.Enrolments en
JOIN  dbo.Users      u   ON u.UserID      = en.ParticipantID
JOIN  dbo.Categories cat ON cat.CategoryID = en.CategoryID
JOIN  dbo.Events     ev  ON ev.EventID    = cat.EventID
LEFT JOIN dbo.Results r  ON r.EnrolmentID = en.EnrolmentID;
GO

-- =============================================================
-- VERIFICATION QUERIES
-- =============================================================
SELECT 'Users'       AS TableName, COUNT(*) AS RowCount FROM dbo.Users
UNION ALL
SELECT 'Events',        COUNT(*) FROM dbo.Events
UNION ALL
SELECT 'Categories',    COUNT(*) FROM dbo.Categories
UNION ALL
SELECT 'EventRoutes',   COUNT(*) FROM dbo.EventRoutes
UNION ALL
SELECT 'Enrolments',    COUNT(*) FROM dbo.Enrolments
UNION ALL
SELECT 'Results',       COUNT(*) FROM dbo.Results;
GO
