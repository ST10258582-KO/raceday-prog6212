# RaceDay – API Endpoint Plan

All routes are prefixed with `/api`. Authentication uses JWT bearer tokens.
Roles: **None** = public, **Any** = any authenticated user, **Organiser** = Organiser role only, **Participant** = Participant role only.

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
