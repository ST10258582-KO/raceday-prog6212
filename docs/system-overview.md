# RaceDay – System Overview

This document provides a high-level description of the RaceDay platform, covering the three-tier architecture, user roles and permissions, the six database entities, and the technology stack.

---

## Three-Tier Architecture

RaceDay is built across three development phases that correspond to the three tiers of the application:

| Tier | Phase | Description |
|------|-------|-------------|
| **Planning (Data)** | Part 1 | Database design, entity relationship diagram, API specification, and SQL Server schema. All deliverables are in the `/docs` folder. |
| **Back-End (API)** | Part 2 | ASP.NET Core Web API implementing all endpoints defined in the Part 1 specification. Handles authentication, business logic, and database access. |
| **Front-End (UI)** | Part 3 | ASP.NET MVC web application consuming the Part 2 API. Provides the user-facing interface for Organisers and Participants. |

Each tier is independent and communicates through well-defined contracts: the SQL schema defines the data contract, the API endpoint plan defines the service contract, and the MVC views consume the API.

---

## User Roles and Permissions

All users are stored in the single `Users` table. The `Role` column distinguishes the two account types.

### Organiser
An Organiser is a race or event director who manages events on the platform.

**Permissions:**
- Create, update, and delete events that they own.
- Add, update, and delete race categories within their own events.
- Add and update route information for their own events.
- View all participants enrolled in their events.
- Record and update participant results after an event.

### Participant
A Participant is a runner, walker, or cyclist who enters events.

**Permissions:**
- Register an account and manage their own profile.
- Browse all publicly listed upcoming and open events.
- Enrol in a specific race category (subject to capacity and payment).
- View and cancel their own enrolments.
- View their own results history.

An Organiser cannot enrol in events; a Participant cannot create events. Role is enforced at both the database level (CHECK constraint) and the API level (JWT role claims).

---

## Six Database Entities

| Entity | Purpose | Key Relationships |
|--------|---------|-------------------|
| **Users** | Stores all accounts (Organisers and Participants). | One User (Organiser) → many Events; one User (Participant) → many Enrolments. |
| **Events** | Represents a scheduled race or cycling event. | One Event → many Categories; one Event → one optional EventRoute. |
| **Categories** | Defines the specific race distances and types within an event (e.g. 5K Run, 10K Race). | One Category → many Enrolments. Cascade-deleted with its parent Event. |
| **EventRoutes** | Holds optional map, GPX, and elevation data for an event. | One-to-one with Events. Cascade-deleted with its parent Event. |
| **Enrolments** | Records a Participant's registration for a specific Category. | Links Users (Participant) and Categories. One Enrolment → one optional Result. |
| **Results** | Stores the finish time, position, and outcome for a completed Enrolment. | One-to-one with Enrolments. Entered by the Organiser after the event. |

The data flows from creation to completion in the following order:
1. An Organiser creates an **Event**.
2. The Organiser adds one or more **Categories** to the Event.
3. Optionally, the Organiser adds an **EventRoute**.
4. Participants create **Enrolments** in specific Categories.
5. After the event, the Organiser records **Results** against each Enrolment.

---

## Technology Stack

| Component | Technology | Notes |
|-----------|-----------|-------|
| Database | Microsoft SQL Server 2019+ | Hosted on a local or cloud SQL Server instance. SSMS used for administration. |
| Back-End API | ASP.NET Core Web API (.NET 8) | RESTful API with JWT bearer authentication. Entity Framework Core for ORM. |
| Front-End | ASP.NET Core MVC (.NET 8) | Razor views consuming the API via HTTP client. Role-based view rendering. |
| Authentication | JWT (JSON Web Tokens) | Tokens issued on login; role claims used for access control throughout the API. |
| Version Control | Git / GitHub | Repository hosted on GitHub with CI/CD via GitHub Actions. |
| CI/CD | GitHub Actions | Workflow validates that all required planning documents are present in `/docs` on every push. |
