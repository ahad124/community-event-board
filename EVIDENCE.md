# Evidence — Acceptance Criteria Verification

All criteria were verified against the stack running locally via
`docker-compose up --build`, immediately after a clean `docker-compose down -v`.
Evidence below is captured API request/response output (status codes + payloads)
plus UI confirmation from the running frontend at `http://localhost`.

- Date verified: 2026-07-14
- App: http://localhost · API: http://localhost:8080 (also `http://localhost/api`)
- Accounts: `admin@eventboard.com` / `Admin123!`, `alice@example.com` / `Alice123!`

## Results Summary

| AC | Criterion | Result |
|----|-----------|--------|
| AC1 | Register + login returns JWT | ✅ PASS |
| AC2 | Protected endpoint without token → 401 | ✅ PASS |
| AC3 | Seeded admin can access admin endpoints | ✅ PASS |
| AC4 | Normal user → admin endpoint → 403 | ✅ PASS |
| AC5 | List shows ≥2 events (title/date/location/thumbnail) | ✅ PASS |
| AC6 | Event detail shows weather for the city | ✅ PASS |
| AC7 | Weather from external API (temp + description) | ✅ PASS |
| AC8 | Authenticated user creates event; appears in list | ✅ PASS |
| AC9 | Image upload saved to filesystem; shown as thumbnail | ✅ PASS |
| AC10 | Full-size image on detail page | ✅ PASS |
| AC11 | RSVP "yes" updates count | ✅ PASS |
| AC12 | Change RSVP yes→no updates count | ✅ PASS |
| AC13 | Unauthenticated cannot RSVP (UI + API 401) | ✅ PASS |
| AC14 | "My Events" lists created events | ✅ PASS |
| AC15 | "My RSVPs" lists responded events | ✅ PASS |
| AC16 | Admin lists all users + roles | ✅ PASS |
| AC17 | Admin promotes user; takes effect | ✅ PASS |
| AC18 | Admin disables user; login → 401 | ✅ PASS |
| AC19 | Admin edits and deletes any event | ✅ PASS |
| AC20 | Dashboard counts + RSVP total per event | ✅ PASS |
| AC21 | 3 services start; API connects to DB | ✅ PASS |
| AC22 | Reachable at http://localhost after down -v | ✅ PASS |
| AC23 | Uploaded images survive container restart | ✅ PASS |
| AC24 | README with docker-compose + architecture | ✅ PASS |
| AC25 | ADRs/ folder with ≥3 ADRs | ✅ PASS (9) |
| AC26 | PROMPTS.md with 20 prompts | ✅ PASS |
| AC27 | UAT-PLAN.md + EVIDENCE.md | ✅ PASS |

---

## Authentication & Authorisation

```
--- AC1: register unique user + login returns JWT ---
register:   HTTP 200
login JWT length: 427 (non-empty => token issued)
--- AC2: POST /events without token => 401 ---
  HTTP 401
--- AC3: admin login + admin endpoint ---
  admin token length: 427
  GET /users as admin: HTTP 200
--- AC4: normal user hits admin endpoint => 403 ---
  GET /users as normal user: HTTP 403
```

## Event Browsing

```
--- AC5: events with title/date/location ---
  event count: 10
   - Global Tech Summit 2026 | 2026-08-14 | San Francisco, CA
   - Intro to React & Vite Workshop | 2026-07-29 | Austin, TX
   - AI & Machine Learning meetup | 2026-07-21 | New York, NY
--- AC6/AC7: weather for a city event (current temp + description + forecast) ---
  city: San Francisco | available: True | temp: 16.3 C | desc: broken clouds | forecast days: 4
```

UI (event detail page, anonymous visitor) — from the live frontend:

```
CURRENT WEATHER
16°C  Broken Clouds
San Francisco · 75% humidity
FORECAST
Tue 16°C   Wed 14°C   Thu 14°C   Fri 14°C
RSVPS (2)   Yes 1   Maybe 0   No 1
RSVP:  [ Sign In to RSVP ]
```

The events list renders a placeholder thumbnail (calendar icon on a gradient) for
events without an uploaded image, and the uploaded image for those with one (AC5/AC9).

## Event Creation & Image Upload

```
--- AC9: upload a PNG image ---
  uploaded imageUrl: /uploads/fe4c65e15cd34219ae9a1fb8e10c95fd.png
  file present in container wwwroot/uploads: fe4c65e15cd34219ae9a1fb8e10c95fd.png
  image served over HTTP: GET /uploads/...png => HTTP 200
--- AC8/AC10: create event with image; appears in list; detail shows image ---
  created event id: 11
  in public list: Verified Event ... | imageUrl set: True
  detail imageUrl: /uploads/fe4c65e15cd34219ae9a1fb8e10c95fd.png
```

## RSVP

```
--- AC13: unauthenticated RSVP rejected ---
  POST /bookings no token => HTTP 401     (UI shows "Sign In to RSVP", no yes/maybe/no buttons)
--- AC11: RSVP Yes -> count ---
  after Yes -> total 1 yes 1 no 0
--- AC12: change Yes -> No ---
  after No  -> total 1 yes 0 no 1
```

## User Dashboard

```
--- AC14: /events/mine ---
  my created events: ['Verified Event ...']
--- AC15: /bookings/my ---
  my RSVPs: [('Global Tech Summit 2026','Yes'), ('Intro to React & Vite Workshop','Maybe'), ('Verified Event ...','No')]
```

## Admin Panel

```
--- AC16: list all users + roles ---
   admin@eventboard.com - Admin - active
   alice@example.com - User - active
   bob@example.com - User - active
--- AC17: promote a fresh user to Admin; user then accesses admin endpoint after re-login ---
  before promote, GET /users as that user: HTTP 403
  promote PUT /users/{id}/role => HTTP 200
  after promote + re-login, GET /users as that user: HTTP 200
--- AC18: disable a user => that user login rejected (401) ---
  disable PUT /users/{id}/status => HTTP 200
  disabled user login => HTTP 401
--- AC19: admin edits and deletes an event ---
  PUT edit title => HTTP 200   (new title: "Admin Edited Title")
  DELETE event => HTTP 204
  GET deleted event => HTTP 404
--- AC20: dashboard stats + per-event RSVP total ---
  stats: totalUsers=5, totalEvents=11, totalRsvps=5, yes=2, maybe=1, no=2
  per-event RSVP report:
    - Global Tech Summit 2026    total 2  yes 1
    - AI & Machine Learning meetup total 1  yes 1
    - Intro to React & Vite Workshop total 1 yes 0
```

The admin dashboard shows summary stat cards (users/events/categories/RSVPs/favorites)
and an "RSVPs" column per event in the events table.

## Docker & Environment

```
--- AC21: three services running; API connected to DB ---
  api: Up (healthy stack)
  frontend: Up
  sqlserver: Up (healthy)
  eventboard-api | Database migrated and seeded successfully.
--- AC22: app reachable at http://localhost (fresh state after down -v && up --build) ---
  GET http://localhost/ => HTTP 200
  seeded events visible: 11
--- AC23: uploaded image survives container restart (mounted volume) ---
  before restart GET /uploads/...png => HTTP 200
  (docker compose restart api)
  after restart GET /uploads/...png => HTTP 200   (file still present in uploads_data volume)
```

## Repository & Documentation

```
--- AC24: README with docker-compose + architecture --- YES
--- AC25: ADRs/ folder --- 9 ADRs (frontend, auth, file storage, DB, docker, weather, RSVP, ...)
--- AC26: PROMPTS.md --- 20 numbered prompt entries
--- AC27: UAT-PLAN.md + EVIDENCE.md --- present (this file)
```

---

### How to reproduce

```bash
git clone https://github.com/ahad124/community-event-board.git
cd community-event-board
cp .env.example .env          # add OPENWEATHER_API_KEY for live weather
docker-compose down -v
docker-compose up --build
# open http://localhost
```
