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
-- Stores both Organisers and Participants, distinguished by Role.
-- =============================================================
CREATE TABLE dbo.Users (
    UserID        INT            IDENTITY(1,1)  NOT NULL,
    FullName      NVARCHAR(100)                 NOT NULL,
    Email         NVARCHAR(150)                 NOT NULL,
    PasswordHash  NVARCHAR(256)                 NOT NULL,
    Role          NVARCHAR(20)                  NOT NULL,
    PhoneNumber   NVARCHAR(20)                  NULL,
    DateOfBirth   DATE                          NULL,
    CreatedAt     DATETIME2                     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE(),
    IsActive      BIT                           NOT NULL CONSTRAINT DF_Users_IsActive  DEFAULT 1,

    CONSTRAINT PK_Users        PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_Email  UNIQUE      (Email),
    CONSTRAINT CK_Users_Role   CHECK       (Role IN ('Organiser', 'Participant'))
);
GO

-- =============================================================
-- TABLE: Events
-- Road running, walking, or cycling events created by Organisers.
-- =============================================================
CREATE TABLE dbo.Events (
    EventID      INT            IDENTITY(1,1)  NOT NULL,
    OrganiserID  INT                           NOT NULL,
    Name         NVARCHAR(150)                 NOT NULL,
    Description  NVARCHAR(MAX)                 NULL,
    EventDate    DATE                          NOT NULL,
    Location     NVARCHAR(200)                 NOT NULL,
    City         NVARCHAR(100)                 NOT NULL,
    Province     NVARCHAR(100)                 NOT NULL,
    Status       NVARCHAR(20)                  NOT NULL CONSTRAINT DF_Events_Status DEFAULT 'Upcoming',
    ImageURL     NVARCHAR(500)                 NULL,
    CreatedAt    DATETIME2                     NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Events           PRIMARY KEY (EventID),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT CK_Events_Status    CHECK       (Status IN ('Upcoming', 'Open', 'Closed', 'Completed', 'Cancelled'))
);
GO

-- =============================================================
-- TABLE: Categories
-- Race categories within an event (e.g. 5K Run, Half Marathon).
-- =============================================================
CREATE TABLE dbo.Categories (
    CategoryID       INT            IDENTITY(1,1)  NOT NULL,
    EventID          INT                           NOT NULL,
    Name             NVARCHAR(100)                 NOT NULL,
    Distance         DECIMAL(6, 2)                 NOT NULL,
    DistanceUnit     NVARCHAR(10)                  NOT NULL CONSTRAINT DF_Categories_Unit    DEFAULT 'km',
    EntryFee         DECIMAL(10, 2)                NOT NULL CONSTRAINT DF_Categories_Fee     DEFAULT 0.00,
    MaxParticipants  INT                           NULL,
    EventType        NVARCHAR(20)                  NOT NULL,
    CreatedAt        DATETIME2                     NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Categories           PRIMARY KEY (CategoryID),
    CONSTRAINT FK_Categories_Event     FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID) ON DELETE CASCADE,
    CONSTRAINT CK_Categories_Unit      CHECK       (DistanceUnit IN ('km', 'm')),
    CONSTRAINT CK_Categories_Type      CHECK       (EventType IN ('Running', 'Walking', 'Cycling')),
    CONSTRAINT CK_Categories_Distance  CHECK       (Distance > 0),
    CONSTRAINT CK_Categories_Fee       CHECK       (EntryFee >= 0)
);
GO

-- =============================================================
-- TABLE: EventRoutes
-- Optional route/map data for an event. One-to-one with Events.
-- =============================================================
CREATE TABLE dbo.EventRoutes (
    RouteID           INT            IDENTITY(1,1)  NOT NULL,
    EventID           INT                           NOT NULL,
    RouteDescription  NVARCHAR(MAX)                 NULL,
    MapURL            NVARCHAR(500)                 NULL,
    GPXData           NVARCHAR(MAX)                 NULL,
    ElevationGain     DECIMAL(8, 2)                 NULL,
    CreatedAt         DATETIME2                     NOT NULL CONSTRAINT DF_Routes_CreatedAt DEFAULT GETDATE(),

    CONSTRAINT PK_EventRoutes       PRIMARY KEY (RouteID),
    CONSTRAINT FK_Routes_Event      FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID) ON DELETE CASCADE,
    CONSTRAINT UQ_Routes_EventID    UNIQUE      (EventID)
);
GO

-- =============================================================
-- TABLE: Enrolments
-- A Participant's entry into a specific Category.
-- Unique constraint prevents double-entry per participant per category.
-- =============================================================
CREATE TABLE dbo.Enrolments (
    EnrolmentID    INT            IDENTITY(1,1)  NOT NULL,
    ParticipantID  INT                           NOT NULL,
    CategoryID     INT                           NOT NULL,
    EnrolmentDate  DATETIME2                     NOT NULL CONSTRAINT DF_Enrolments_Date  DEFAULT GETDATE(),
    PaymentStatus  NVARCHAR(20)                  NOT NULL CONSTRAINT DF_Enrolments_Pay   DEFAULT 'Pending',
    BibNumber      NVARCHAR(20)                  NULL,

    CONSTRAINT PK_Enrolments              PRIMARY KEY (EnrolmentID),
    CONSTRAINT FK_Enrolments_Participant  FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Category     FOREIGN KEY (CategoryID)    REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Enrolment_ParticipantCategory UNIQUE (ParticipantID, CategoryID),
    CONSTRAINT CK_Enrolments_PayStatus    CHECK       (PaymentStatus IN ('Pending', 'Paid', 'Refunded'))
);
GO

-- =============================================================
-- TABLE: Results
-- Finish time and position recorded by Organiser after the event.
-- One-to-one with Enrolments.
-- =============================================================
CREATE TABLE dbo.Results (
    ResultID      INT            IDENTITY(1,1)  NOT NULL,
    EnrolmentID   INT                           NOT NULL,
    FinishTime    TIME                          NULL,
    Position      INT                           NULL,
    Status        NVARCHAR(20)                  NOT NULL CONSTRAINT DF_Results_Status DEFAULT 'DNS',
    Notes         NVARCHAR(500)                 NULL,
    RecordedAt    DATETIME2                     NOT NULL CONSTRAINT DF_Results_RecordedAt DEFAULT GETDATE(),

    CONSTRAINT PK_Results            PRIMARY KEY (ResultID),
    CONSTRAINT FK_Results_Enrolment  FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID),
    CONSTRAINT UQ_Results_Enrolment  UNIQUE      (EnrolmentID),
    CONSTRAINT CK_Results_Status     CHECK       (Status IN ('Finished', 'DNF', 'DNS', 'DQ')),
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
