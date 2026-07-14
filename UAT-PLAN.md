# UAT Plan — Community Event Board

This plan maps every acceptance criterion (AC1–AC27) to a concrete test performed
against the application running locally via `docker-compose up --build`.

- **App URL:** http://localhost
- **API URL:** http://localhost:8080 (also proxied under http://localhost/api)
- **Seeded accounts:** `admin@eventboard.com` / `Admin123!`, `alice@example.com` / `Alice123!`, `bob@example.com` / `Bob123!`

See [`EVIDENCE.md`](EVIDENCE.md) for captured request/response output proving each
result. A detailed manual UAT script is in
[`EventBoard.Api.Tests/UAT.md`](EventBoard.Api.Tests/UAT.md).

## Authentication & Authorisation

| AC | Test | Expected |
|----|------|----------|
| AC1 | `POST /api/auth/register` with a unique email, then `POST /api/auth/login` | Registration 200; login returns a JWT |
| AC2 | `POST /api/events` with no token | `401 Unauthorized` |
| AC3 | Login as `admin@eventboard.com`; call `GET /api/users` | 200 with user list |
| AC4 | Login as a normal user; call `GET /api/users` | `403 Forbidden` |

## Event Browsing

| AC | Test | Expected |
|----|------|----------|
| AC5 | `GET /api/events` (no auth); open the list page | ≥2 events with title, date, location, thumbnail (placeholder when no image) |
| AC6 | Open an event detail page | Full details + weather for the event's city |
| AC7 | `GET /api/weather/event/{id}` for a city event | Current temperature + description (+ short forecast) |

## Event Creation & Image Upload

| AC | Test | Expected |
|----|------|----------|
| AC8 | As an authenticated user, create an event via the form | Event appears in the public list |
| AC9 | Upload a jpg/png during creation | Image stored under `wwwroot/uploads` (volume) and shown as a thumbnail |
| AC10 | Open the created event's detail page | Full-size image displayed |

## RSVP

| AC | Test | Expected |
|----|------|----------|
| AC11 | RSVP "Yes" to an event | Detail page RSVP count increases |
| AC12 | Change RSVP "Yes" → "No" | Counts update correctly (yes−1, no+1) |
| AC13 | Visitor (no token) tries to RSVP | Buttons replaced by "Sign In to RSVP"; `POST /api/bookings` → 401 |

## User Dashboard

| AC | Test | Expected |
|----|------|----------|
| AC14 | `GET /api/events/mine`; dashboard "My Created Events" | Lists events the user created |
| AC15 | `GET /api/bookings/my`; dashboard "My RSVPs" | Lists events the user responded to |

## Admin Panel

| AC | Test | Expected |
|----|------|----------|
| AC16 | Admin → Manage Users (`GET /api/users`) | All users + roles listed |
| AC17 | Admin promotes a user (`PUT /api/users/{id}/role`) | Role becomes Admin; user gains admin access after re-login |
| AC18 | Admin disables a user (`PUT /api/users/{id}/status`); that user logs in | Login rejected with `401` |
| AC19 | Admin edits (`PUT /api/events/{id}`) and deletes (`DELETE`) any event | Changes applied |
| AC20 | Admin dashboard stats + per-event RSVP column | Total users, total events, and RSVP total per event shown |

## Docker & Environment

| AC | Test | Expected |
|----|------|----------|
| AC21 | `docker-compose up --build` | sqlserver + api + frontend start; API connects to DB |
| AC22 | After `docker-compose down -v` then `up --build`, use the app at `http://localhost` | All criteria exercisable in a fresh browser |
| AC23 | Upload an image, then `docker compose restart api` | Image still served (mounted volume) |

## Repository & Documentation

| AC | Test | Expected |
|----|------|----------|
| AC24 | Inspect `README.md` | Setup + docker-compose commands + architecture |
| AC25 | Inspect `ADRs/` | ≥3 ADRs (frontend, auth, file storage, …) |
| AC26 | Inspect `PROMPTS.md` | 20 prompts with notes |
| AC27 | Inspect `UAT-PLAN.md` + `EVIDENCE.md` | Plan covers ACs; evidence shows each met |
