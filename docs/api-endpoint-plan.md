# RaceDay – API Endpoint Plan

All routes are prefixed with `/api`. Authentication uses JWT bearer tokens.
Roles: **None** = public, **Any** = any authenticated user, **Organiser** = Organiser role only, **Participant** = Participant role only.

---

## Design Principles

### Authentication Strategy
All protected endpoints require a JWT (JSON Web Token) bearer token in the `Authorization` header (`Authorization: Bearer <token>`). Tokens are issued on successful login and registration. Tokens carry the user's ID and role as claims, allowing the API to make access-control decisions without a database round-trip on every request. Token expiry is set to 24 hours; clients must re-authenticate once the token expires. Refresh token support is planned for Part 2.

### Role Enforcement
Role-based access is enforced at two layers. At the database level, the `Role` column uses a CHECK constraint limiting values to `'Organiser'` or `'Participant'`. At the API level, every protected endpoint checks the role claim embedded in the JWT. Endpoints marked **Organiser** reject requests from Participants with `403 Forbidden`. Endpoints marked **Participant** reject requests from Organisers with `403 Forbidden`. Ownership checks (e.g. an Organiser may only modify their own events) are validated against the `OrganiserID` field on the event record.

### Error Handling Conventions
All error responses return a consistent JSON envelope:
```json
{
  "status": 400,
  "error": "Bad Request",
  "message": "EventDate must be a future date."
}
```
Validation errors include a `errors` array with field-level detail. The API never exposes internal stack traces or database error messages to clients.

### RESTful Naming Conventions
Resources are named using lowercase plural nouns (`/events`, `/categories`, `/enrolments`, `/results`). Sub-resources are expressed as nested paths (`/events/{id}/categories`). HTTP verbs carry the action meaning (GET = read, POST = create, PUT = full or partial update, DELETE = remove). Route parameters use camelCase (`{eventId}`, `{categoryId}`).

### Request and Response Format
All request bodies and response bodies use JSON (`Content-Type: application/json`). Dates are formatted as ISO 8601 strings (`YYYY-MM-DD` for dates, `HH:MM:SS` for times). Monetary values are represented as decimal numbers. Arrays are returned directly as the response body (not wrapped in an object) except where pagination metadata is included.

---

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| POST | /api/auth/register | Registers a new user account. Accepts either Organiser or Participant role. | None | `{ fullName, email, password, role, phoneNumber?, dateOfBirth? }` | 201 Created – user object with JWT token. 400 Bad Request – validation error. 409 Conflict – email already exists. |
| POST | /api/auth/login | Authenticates an existing user and returns a JWT token. | None | `{ email, password }` | 200 OK – user object with JWT token. 400 Bad Request – missing fields. 401 Unauthorized – invalid credentials. |

---

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | /api/users/me | Returns the authenticated user's own profile. | Any | None | 200 OK – user profile object. 401 Unauthorized – not logged in. |
| PUT | /api/users/me | Updates the authenticated user's own profile details. | Any | `{ fullName?, phoneNumber?, dateOfBirth? }` | 200 OK – updated user object. 400 Bad Request – validation error. 401 Unauthorized. |

---

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | /api/events | Returns a list of all upcoming and open events. Publicly accessible. | None | None | 200 OK – array of event objects. |
| GET | /api/events/{id} | Returns full details for a single event, including its categories. | None | None | 200 OK – event detail object. 404 Not Found – event does not exist. |
| POST | /api/events | Creates a new event. The authenticated Organiser is set as the owner. | Organiser | `{ name, description?, eventDate, location, city, province, imageURL? }` | 201 Created – new event object. 400 Bad Request – validation error. 401/403 – not an Organiser. |
| PUT | /api/events/{id} | Updates an existing event. Only the owning Organiser may update it. | Organiser | `{ name?, description?, eventDate?, location?, city?, province?, status?, imageURL? }` | 200 OK – updated event object. 403 Forbidden – not the event owner. 404 Not Found. |
| DELETE | /api/events/{id} | Deletes an event and all its categories. Only the owning Organiser may delete. | Organiser | None | 204 No Content. 403 Forbidden – not the event owner. 404 Not Found. |
| GET | /api/events/{id}/enrolments | Returns all enrolments for an event, grouped by category. Allows Organiser to see who has entered. | Organiser | None | 200 OK – array of enrolment objects with participant details. 403 Forbidden. 404 Not Found. |
| GET | /api/events/{id}/results | Returns published results for an event. Publicly accessible. | None | None | 200 OK – array of result objects. 404 Not Found. |

---

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | /api/events/{id}/categories | Returns all race categories for a given event. | None | None | 200 OK – array of category objects. 404 Not Found – event does not exist. |
| GET | /api/events/{eventId}/categories/{categoryId} | Returns details for a single category within an event. | None | None | 200 OK – category object. 404 Not Found. |
| POST | /api/events/{id}/categories | Adds a new race category to an event. Only the event's Organiser may add categories. | Organiser | `{ name, distance, distanceUnit?, entryFee, maxParticipants?, eventType }` | 201 Created – new category object. 400 Bad Request – validation error. 403 Forbidden. 404 Not Found – event does not exist. |
| PUT | /api/events/{eventId}/categories/{categoryId} | Updates an existing category. Only the event's Organiser may update. | Organiser | `{ name?, distance?, distanceUnit?, entryFee?, maxParticipants?, eventType? }` | 200 OK – updated category object. 403 Forbidden. 404 Not Found. |
| DELETE | /api/events/{eventId}/categories/{categoryId} | Deletes a category from an event. Only the event's Organiser may delete. | Organiser | None | 204 No Content. 403 Forbidden. 404 Not Found. |

---

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| POST | /api/enrolments | Enrols the authenticated Participant into a specific event category. | Participant | `{ categoryId }` | 201 Created – enrolment object with payment status. 400 Bad Request – already enrolled or category full. 403 Forbidden – not a Participant. 404 Not Found – category does not exist. |
| GET | /api/enrolments/my | Returns all enrolments belonging to the authenticated Participant. | Participant | None | 200 OK – array of enrolment objects including event and category details. 401 Unauthorized. |
| GET | /api/enrolments/{id} | Returns a single enrolment record. Participant may only access their own; Organiser may access any. | Any | None | 200 OK – enrolment object. 403 Forbidden – accessing another user's enrolment. 404 Not Found. |
| DELETE | /api/enrolments/{id} | Cancels a Participant's enrolment. Participant may only cancel their own. | Participant | None | 204 No Content. 403 Forbidden. 404 Not Found. |

---

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| POST | /api/results | Records a result for a specific enrolment after the event. Organiser must own the event. | Organiser | `{ enrolmentId, finishTime?, position?, status, notes? }` | 201 Created – result object. 400 Bad Request – result already exists for this enrolment. 403 Forbidden. 404 Not Found – enrolment does not exist. |
| PUT | /api/results/{id} | Updates an existing result record (e.g. corrects finish time or position). | Organiser | `{ finishTime?, position?, status?, notes? }` | 200 OK – updated result object. 403 Forbidden. 404 Not Found. |
| GET | /api/results/my | Returns all results for the authenticated Participant's past enrolments. | Participant | None | 200 OK – array of result objects with event and category context. 401 Unauthorized. |

---

## Event Routes

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | /api/events/{id}/route | Returns route and map information for an event. Publicly accessible. | None | None | 200 OK – route object. 404 Not Found – event or route does not exist. |
| POST | /api/events/{id}/route | Adds route information to an event. Only the event's Organiser may add a route. | Organiser | `{ routeDescription?, mapURL?, gpxData?, elevationGain? }` | 201 Created – route object. 400 Bad Request – route already exists (use PUT). 403 Forbidden. 404 Not Found. |
| PUT | /api/events/{id}/route | Updates route information for an event. Only the event's Organiser may update. | Organiser | `{ routeDescription?, mapURL?, gpxData?, elevationGain? }` | 200 OK – updated route object. 403 Forbidden. 404 Not Found. |

---

## HTTP Status Code Reference

| Code | Meaning       | When Used                                                                                         |
|------|---------------|---------------------------------------------------------------------------------------------------|
| 200  | OK            | The request succeeded and the response body contains the requested data (GET, PUT).               |
| 201  | Created       | A new resource was successfully created (POST). The response body contains the new resource.      |
| 204  | No Content    | The request succeeded but there is no response body to return (DELETE).                           |
| 400  | Bad Request   | The request body failed validation (missing required fields, invalid values, business rule breach).|
| 401  | Unauthorized  | No valid JWT token was provided, or the token has expired. The client must log in again.          |
| 403  | Forbidden     | A valid token was provided but the user does not have the required role or does not own the resource. |
| 404  | Not Found     | The requested resource does not exist (invalid ID, deleted record).                               |
| 409  | Conflict      | The request would create a duplicate that violates a unique constraint (e.g. email already registered, participant already enrolled in category). |
| 500  | Internal Server Error | An unexpected server-side error occurred. The client should retry or contact support.     |
