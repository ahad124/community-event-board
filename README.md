# Community Event Board

A full-stack application for discovering, creating and RSVPing to local events. It
has a public browsing area, authenticated event creation, RSVP (yes / maybe / no),
a weather forecast for each event's city, and a role-based admin panel. The entire
stack runs locally with a single `docker-compose up`.

- **Frontend:** React 19 + Vite 8 + Bootstrap 5 (client-side rendered)
- **Backend:** ASP.NET Core 8 Web API + Entity Framework Core (SQL Server)
- **Database:** SQL Server 2022 (in Docker), schema managed by EF Core migrations
- **Auth:** JWT bearer tokens with role-based access control (`Admin` / `User`)
- **Third-party API:** OpenWeatherMap (current conditions + short forecast)

---

## Table of Contents

- [Quick Start (Docker)](#quick-start-docker)
- [Architecture](#architecture)
- [Repository Layout](#repository-layout)
- [Tech Stack](#tech-stack)
- [Seed Data & Test Accounts](#seed-data--test-accounts)
- [Features](#features)
- [API Reference](#api-reference)
- [Data Model](#data-model)
- [Configuration](#configuration)
- [Local Development (without Docker)](#local-development-without-docker)
- [Testing](#testing)
- [Further Documentation](#further-documentation)

---

## Quick Start (Docker)

**Prerequisite:** Docker Desktop (Docker Engine + Compose v2).

```bash
# 1. (optional) configure secrets — otherwise sensible defaults are used
cp .env.example .env
#    edit .env to add your free OpenWeatherMap API key (weather works without it,
#    it just shows "unavailable")

# 2. build & start the whole stack (SQL Server + API + frontend)
docker-compose up --build
```

Then open **http://localhost**.

| Service    | URL                          | Notes                              |
|------------|------------------------------|------------------------------------|
| Frontend   | http://localhost             | Nginx-served React SPA             |
| API        | http://localhost:8080        | ASP.NET Core Web API               |
| Swagger    | http://localhost:8080/swagger| Interactive API docs               |
| SQL Server | localhost:1433               | `sa` / password from `.env`        |

The API waits for SQL Server, applies EF Core migrations, and seeds data on first
run. Uploaded images and the database are stored in named Docker volumes, so they
survive restarts.

**Reset everything (wipe volumes) and rebuild:**

```bash
docker-compose down -v
docker-compose up --build
```

---

## Architecture

```
┌──────────────────────────┐      ┌──────────────────────────┐      ┌─────────────────┐
│  frontend (Nginx :80)    │      │  api (Kestrel :8080)     │      │  sqlserver :1433│
│  React 19 + Vite SPA     │──────▶  ASP.NET Core 8 Web API  │──────▶  SQL Server 2022│
│  proxies /api & /uploads │ HTTP │  Controllers → Services  │  EF  │  EventBoardDb   │
│                          │      │  → Repositories → DbCtx  │ Core │                 │
└──────────────────────────┘      └──────────────────────────┘      └─────────────────┘
        http://localhost                                          named volumes:
                                                                  mssql_data, uploads_data
```

Nginx serves the built SPA and reverse-proxies `/api`, `/uploads` and `/swagger`
to the API, so the browser talks to a single origin (`http://localhost`).

---

## Repository Layout

```
community-event-board/
├── docker-compose.yml           # SQL Server + API + frontend
├── .env.example                 # Copy to .env; secrets & API keys
├── README.md
├── PROMPTS.md                   # 20 most impactful AI prompts used
├── ADRs/                        # Architecture Decision Records
│
├── EventBoard.Api/              # ASP.NET Core 8 Web API
│   ├── Dockerfile               # multi-stage build/publish
│   ├── Program.cs               # DI, JWT, CORS, Swagger, migrate+seed
│   ├── Controllers/             # Auth, Events, Categories, Bookings(RSVP),
│   │                            #   Favorites, Users, Reports, Weather
│   ├── Services/                # Auth, Jwt, Weather (+ interfaces)
│   ├── Repositories/            # User/Event/Booking/Favorite (+ interfaces)
│   ├── Models/                  # Entities + DTOs
│   ├── Data/                    # AppDbContext, DbInitializer (migrate + seed)
│   └── Migrations/              # EF Core migrations
│
├── EventBoard.Api.Tests/        # xUnit unit + integration tests, UAT.md
│
└── event-board-frontend/        # React 19 + Vite SPA
    ├── Dockerfile               # Vite build → Nginx
    ├── nginx.conf               # SPA fallback + /api, /uploads proxy
    └── src/
        ├── App.jsx              # Router, NavigationBar, ProtectedRoute
        ├── context/AuthContext.jsx
        └── components/          # EventList, EventDetail, LoginRegister,
                                 #   CreateEvent, UserDashboard, AdminDashboard
```

---

## Tech Stack

| Layer     | Technology                                                              |
|-----------|-------------------------------------------------------------------------|
| Frontend  | React 19, Vite 8, React Router 7, axios, Bootstrap 5.3                   |
| Backend   | ASP.NET Core 8 Web API, EF Core 8, SQL Server                           |
| Auth      | JWT (`JwtBearer`), BCrypt password hashing                              |
| Weather   | OpenWeatherMap (typed `HttpClient` + Polly retry)                       |
| API Docs  | Swagger / OpenAPI (Swashbuckle)                                         |
| Container | Docker + Docker Compose; Nginx for the frontend                        |

---

## Seed Data & Test Accounts

`DbInitializer` seeds five categories, sample events, RSVPs, favorites, and three
users:

| Role  | Email                   | Password    |
|-------|-------------------------|-------------|
| Admin | `admin@eventboard.com`  | `Admin123!` |
| User  | `alice@example.com`     | `Alice123!` |
| User  | `bob@example.com`       | `Bob123!`   |

Log in as **admin** to reach the Admin Panel (`/admin`).

---

## Features

**Public / User area**
- Register & log in (JWT).
- Browse upcoming events (no login required) with category filtering.
- Event detail with the weather forecast for the event's city.
- Create events (any logged-in user) with an uploaded image.
- RSVP **yes / maybe / no**; the detail page shows live RSVP tallies.
- Dashboard: events you created, your RSVPs, and your favorites.

**Admin panel** (`Admin` role only)
- User management: list users, promote/demote admins, enable/disable accounts.
- Event management: view/edit/delete any event, change images.
- Event RSVPs: view responses per event.
- Statistics: totals for users, events, categories, RSVPs (with breakdown) and favorites.

---

## API Reference

All routes are prefixed with `/api`. 🔓 public · 🔒 authenticated · 👑 Admin only.

#### Auth — `/api/auth`
| Method | Route       | Access | Description                    |
|--------|-------------|--------|--------------------------------|
| POST   | `/register` | 🔓     | Register a new user            |
| POST   | `/login`    | 🔓     | Log in (403 if account disabled) |

#### Events — `/api/events`
| Method | Route                    | Access | Description                    |
|--------|--------------------------|--------|--------------------------------|
| GET    | `/`                      | 🔓     | List all events (with RSVP counts) |
| GET    | `/{id}`                  | 🔓     | Get event by id                |
| GET    | `/category/{categoryId}` | 🔓     | List events in a category      |
| GET    | `/mine`                  | 🔒     | Events created by current user |
| POST   | `/`                      | 🔒     | Create event (organizer = JWT) |
| POST   | `/upload-image`          | 🔒     | Upload an event image          |
| PUT    | `/{id}`                  | 👑     | Update any event               |
| DELETE | `/{id}`                  | 👑     | Delete any event               |

#### RSVPs — `/api/bookings`
| Method | Route              | Access | Description                              |
|--------|--------------------|--------|------------------------------------------|
| POST   | `/`                | 🔒     | RSVP yes/maybe/no (upsert)               |
| GET    | `/my`              | 🔒     | Current user's RSVPs                      |
| GET    | `/{id}`            | 🔒     | Get an RSVP by id                         |
| GET    | `/`                | 👑     | All RSVPs                                 |
| GET    | `/event/{eventId}` | 👑     | RSVPs for a specific event               |

#### Users — `/api/users` (👑)
| Method | Route          | Description                     |
|--------|----------------|---------------------------------|
| GET    | `/`            | List all users                  |
| PUT    | `/{id}/role`   | Promote/demote (`User`/`Admin`) |
| PUT    | `/{id}/status` | Enable/disable account          |

#### Categories — `/api/categories`
| Method | Route   | Access | Description     |
|--------|---------|--------|-----------------|
| GET    | `/`     | 🔓     | List categories |
| POST   | `/`     | 👑     | Create category |
| DELETE | `/{id}` | 👑     | Delete category |

#### Favorites — `/api/favorites` (🔒)
`GET /` list · `POST /{eventId}` toggle · `DELETE /{eventId}` remove.

#### Reports — `/api/reports` (👑)
`GET /events` per-event RSVP report · `GET /stats` dashboard summary counts.

#### Weather — `/api/weather`
`GET /event/{eventId}` 🔓 — current conditions + short forecast for the event's city.

---

## Data Model

| Entity          | Key fields                                                              | Notes |
|-----------------|------------------------------------------------------------------------|-------|
| `User`          | `Id` (Guid), `UserName`, `Email` (unique), `PasswordHash`, `Role`, `IsActive` | Roles `User`/`Admin`; disabled users can't log in |
| `Category`      | `Id`, `Name` (unique)                                                   | Delete restricted if events reference it |
| `Event`         | `Id`, `Title`, `Description?`, `Date`, `Location?`, `ImageUrl?`, `CategoryId`, `OrganizerId` | |
| `EventBooking`  | `Id`, `EventId`, `UserId`, `BookingDate`, `Status`                      | RSVP `Status` = `Yes` \| `Maybe` \| `No` |
| `EventFavorite` | `UserId`, `EventId`, `AddedAt`                                          | Composite key join table |

Schema is created and versioned via **EF Core migrations** (`EventBoard.Api/Migrations`),
applied automatically on startup.

---

## Configuration

Configuration is read from `appsettings.json` and overridden by environment
variables (double-underscore syntax). In Docker these are set in `docker-compose.yml`
and `.env`:

| Variable                             | Purpose                                   |
|--------------------------------------|-------------------------------------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string            |
| `Jwt__Key` / `Jwt__Issuer` / `Jwt__Audience` | JWT signing/validation            |
| `OpenWeather__ApiKey`                | OpenWeatherMap key (weather degrades gracefully if empty) |
| `Cors__AllowedOrigins__0`            | Allowed CORS origin(s)                    |
| `MSSQL_SA_PASSWORD`                  | SQL Server `sa` password (compose)        |

The frontend uses `VITE_API_BASE_URL` (default `/api`, proxied by Nginx/Vite).

---

## Local Development (without Docker)

Requires **.NET 8 SDK**, **Node 18+**, and a reachable **SQL Server** instance
(update the connection string in `appsettings.Development.json`).

```bash
# Backend
cd EventBoard.Api
dotnet run                      # http://localhost:5000 (+ /swagger)

# Frontend (separate terminal)
cd event-board-frontend
npm install
npm run dev                     # http://localhost:5173, proxies /api → :5000
```

To create/apply migrations manually:

```bash
cd EventBoard.Api
dotnet ef migrations add <Name>
dotnet ef database update
```

---

## Testing

```bash
cd EventBoard.Api.Tests
dotnet test
```

Includes unit tests (services, with Moq) and integration tests (via
`WebApplicationFactory` against an in-memory database). See
[`EventBoard.Api.Tests/UAT.md`](EventBoard.Api.Tests/UAT.md) for the manual UAT plan.

---

## Further Documentation

- [`PROMPTS.md`](PROMPTS.md) — the 20 most impactful AI prompts used to build this project.
- [`ADRs/`](ADRs/) — Architecture Decision Records.
- [`EventBoard.Api.Tests/UAT.md`](EventBoard.Api.Tests/UAT.md) — UAT test plan.
