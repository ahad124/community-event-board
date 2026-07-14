# EventBoard

A full-stack event management platform. Browse events, register (book) for them, favorite them, and — as an admin — manage events, categories, and moderate bookings.

- **Frontend:** React 19 + Vite 8 + Bootstrap 5 (custom dark "glassmorphism" theme)
- **Backend:** ASP.NET Core 8 Web API + Entity Framework Core + SQLite
- **Auth:** JWT bearer tokens with role-based access control (`Admin` / `User`)

---

## Table of Contents

- [Architecture](#architecture)
- [Repository Layout](#repository-layout)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Seed Data & Test Accounts](#seed-data--test-accounts)
- [Backend](#backend)
  - [Data Model](#data-model)
  - [API Reference](#api-reference)
  - [Authentication & Authorization](#authentication--authorization)
- [Frontend](#frontend)
  - [Routes](#routes)
  - [Components](#components)
  - [Auth State](#auth-state)
  - [Styling & Theme](#styling--theme)
- [Configuration](#configuration)
- [Development Notes](#development-notes)

---

## Architecture

```
┌─────────────────────────────┐         ┌──────────────────────────────┐
│  event-board-frontend       │  HTTP   │  EventBoard.Api              │
│  React 19 + Vite (5173)      │ ──────► │  ASP.NET Core 8 (5000)       │
│                             │  /api   │                              │
│  • React Router (routes)    │  proxy  │  • Controllers (REST)        │
│  • AuthContext (JWT)        │ ◄────── │  • Services (Auth, JWT)      │
│  • axios (API calls)        │  JSON   │  • Repositories (EF Core)    │
│  • Bootstrap + custom CSS   │         │  • AppDbContext → SQLite     │
└─────────────────────────────┘         └──────────────────────────────┘
                                                      │
                                                      ▼
                                            EventBoard.db (SQLite)
```

The Vite dev server proxies all `/api/*` requests to the backend at `http://localhost:5000` (see `event-board-frontend/vite.config.js`), so the frontend and backend can run side by side without CORS friction in development.

---

## Repository Layout

```
React_vite/
├── QUICK_START.md               # Short run guide
├── README.md                    # This file
│
├── EventBoard.Api/              # ASP.NET Core 8 Web API (backend)
│   ├── Program.cs               # App bootstrap: DI, JWT, CORS, Swagger, DB seed
│   ├── appsettings.json         # Connection string + JWT settings
│   ├── EventBoard.Api.csproj    # net8.0, EF Core, JwtBearer, BCrypt, Swashbuckle
│   ├── Controllers/             # AuthController, EventsController, CategoriesController,
│   │                            #   BookingsController, FavoritesController
│   ├── Services/                # AuthService, JwtTokenService (+ interfaces)
│   ├── Repositories/            # User/Event/Booking/Favorite repositories (+ interfaces)
│   ├── Models/                  # User, Event, Category, EventBooking, EventFavorite,
│   │                            #   BookingStatus (enum), AuthResponseDto
│   ├── Data/                    # AppDbContext, DbInitializer (seed)
│   └── EventBoard.db            # SQLite database file (auto-created)
│
└── event-board-frontend/        # React 19 + Vite (frontend)
    ├── vite.config.js           # /api proxy → localhost:5000
    ├── mock-api.js              # Standalone mock server (dev helper, GET /events only)
    ├── package.json             # Scripts: dev, build, preview, lint, mock-api
    └── src/
        ├── main.jsx             # Entry: imports Bootstrap + index.css + App.css
        ├── App.jsx              # Router, NavigationBar, ProtectedRoute, Footer
        ├── index.css            # Base theme overrides
        ├── App.css              # Theme tokens (:root vars) + component styles
        ├── context/
        │   └── AuthContext.jsx  # JWT auth provider + useAuth hook
        └── components/
            ├── EventList.jsx        # Public event grid ("/")
            ├── EventDetail.jsx      # Single event view ("/event/:id")
            ├── LoginRegister.jsx    # Login / registration ("/login")
            ├── UserDashboard.jsx    # "My Bookings" ("/dashboard")
            └── AdminDashboard.jsx   # Admin panel ("/admin")
```

---

## Tech Stack

| Layer      | Technology                                                                 |
|------------|----------------------------------------------------------------------------|
| Frontend   | React 19, Vite 8, React Router 7, axios, Bootstrap 5.3                      |
| Backend    | ASP.NET Core 8 (Web API), Entity Framework Core 8, SQLite                   |
| Auth       | JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`), BCrypt password hash |
| API Docs   | Swagger / OpenAPI (Swashbuckle) — enabled in Development                    |

---

## Getting Started

### Prerequisites

- **Node.js** v18+ (Vite 8 requires a modern Node)
- **.NET 8 SDK**
- SQLite ships with .NET — no separate install needed

### 1. Run the backend

```bash
cd EventBoard.Api
dotnet restore      # first time only
dotnet run
```

The API starts on **http://localhost:5000**. On first run, `DbInitializer.Seed()` creates `EventBoard.db` and populates categories, users, and events. Swagger UI is available at **http://localhost:5000/swagger** in Development.

### 2. Run the frontend

```bash
cd event-board-frontend
npm install         # first time only
npm run dev
```

Open **http://localhost:5173**. API calls to `/api/*` are proxied to the backend automatically.

### Frontend npm scripts

| Script            | Purpose                                             |
|-------------------|-----------------------------------------------------|
| `npm run dev`     | Start Vite dev server (port 5173)                   |
| `npm run build`   | Production build                                    |
| `npm run preview` | Preview the production build                        |
| `npm run lint`    | Run oxlint                                           |
| `npm run mock-api`| Standalone mock server (port 5050, `GET /events` only) — used only when the real backend isn't running |

---

## Seed Data & Test Accounts

`DbInitializer` seeds five categories (Conference, Workshop, Meetup, Concert, Webinar), a set of sample events, and three users:

| Role  | Email                   | Password    |
|-------|-------------------------|-------------|
| Admin | `admin@eventboard.com`  | `Admin123!` |
| User  | `alice@example.com`     | `Alice123!` |
| User  | `bob@example.com`       | `Bob123!`   |

> Log in as **admin** to access the Admin Panel (`/admin`) for managing events, categories, and moderating bookings.

---

## Backend

### Data Model

| Entity          | Key fields                                                                          | Notes |
|-----------------|-------------------------------------------------------------------------------------|-------|
| `User`          | `Id` (Guid), `UserName`, `Email` (unique), `PasswordHash` (BCrypt), `Role`          | Roles: `User` (default), `Admin` |
| `Category`      | `Id` (int), `Name` (unique)                                                         | Deletion **restricted** if events reference it |
| `Event`         | `Id` (int), `Title`, `Description?`, `Date`, `Location?`, `CategoryId`, `OrganizerId`| Indexed on `Date` and `CategoryId` |
| `EventBooking`  | `Id` (int), `EventId`, `UserId`, `BookingDate`, `Status`                            | `Status` = `Pending` \| `Confirmed` \| `Cancelled` |
| `EventFavorite` | `UserId`, `EventId`, `AddedAt`                                                      | Join between users and favorited events |

**Relationships** (configured in `Data/AppDbContext.cs`):

- `User` 1—* `Event` (as organizer) — cascade delete
- `Category` 1—* `Event` — **restrict** delete (can't delete a category with events)
- `Event` 1—* `EventBooking` — cascade delete
- `User` 1—* `EventBooking` — cascade delete
- `User`/`Event` 1—* `EventFavorite` — cascade delete

### API Reference

All routes are prefixed with `/api`. 🔓 = public, 🔒 = authenticated, 👑 = Admin only.

#### Auth — `/api/auth`
| Method | Route            | Access | Description                          |
|--------|------------------|--------|--------------------------------------|
| POST   | `/register`      | 🔓     | Register a new user, returns JWT     |
| POST   | `/login`         | 🔓     | Log in, returns JWT + role           |

#### Events — `/api/events`
| Method | Route                   | Access | Description                     |
|--------|-------------------------|--------|---------------------------------|
| GET    | `/`                     | 🔓     | List all events                 |
| GET    | `/{id}`                 | 🔓     | Get event by id                 |
| GET    | `/category/{categoryId}`| 🔓     | List events in a category       |
| POST   | `/`                     | 👑     | Create event                    |
| PUT    | `/{id}`                 | 👑     | Update event                    |
| DELETE | `/{id}`                 | 👑     | Delete event                    |

#### Categories — `/api/categories`
| Method | Route     | Access | Description        |
|--------|-----------|--------|--------------------|
| GET    | `/`       | 🔓     | List categories    |
| POST   | `/`       | 👑     | Create category    |
| DELETE | `/{id}`   | 👑     | Delete category    |

#### Bookings — `/api/bookings`
| Method | Route              | Access | Description                                   |
|--------|--------------------|--------|-----------------------------------------------|
| POST   | `/`                | 🔒     | Book an event for the current user            |
| GET    | `/my`              | 🔒     | Current user's bookings                        |
| GET    | `/{id}`            | 🔒     | Get a booking by id                            |
| GET    | `/`                | 👑     | All bookings (moderation)                      |
| GET    | `/event/{eventId}` | 👑     | Bookings for a specific event                  |
| PUT    | `/{id}/status`     | 👑     | Update booking status (`Pending`/`Confirmed`/`Cancelled`) |

#### Favorites — `/api/favorites`
| Method | Route          | Access | Description                        |
|--------|----------------|--------|------------------------------------|
| GET    | `/`            | 🔒     | Current user's favorites           |
| POST   | `/{eventId}`   | 🔒     | Toggle a favorite                  |
| DELETE | `/{eventId}`   | 🔒     | Remove a favorite                  |

### Authentication & Authorization

- Passwords are hashed with **BCrypt** (`AuthService`).
- On login/register, `JwtTokenService` issues a signed JWT containing the user id (`sub`), email, and role claim.
- Protected endpoints use `[Authorize]`; admin actions use `[Authorize(Roles = "Admin")]`.
- JWT validation params (issuer, audience, signing key) come from `appsettings.json` → `Jwt` section.

---

## Frontend

### Routes

Defined in `src/App.jsx`. `ProtectedRoute` redirects unauthenticated users to `/login`; passing `allowedRoles` further restricts by role.

| Path          | Component        | Access          |
|---------------|------------------|-----------------|
| `/`           | `EventList`      | Authenticated   |
| `/event/:id`  | `EventDetail`    | Authenticated   |
| `/login`      | `LoginRegister`  | Public          |
| `/dashboard`  | `UserDashboard`  | Authenticated   |
| `/admin`      | `AdminDashboard` | Admin only      |

### Components

| Component          | Responsibility                                                              |
|--------------------|-----------------------------------------------------------------------------|
| `EventList`        | Grid of all events with category filtering and favorite toggles             |
| `EventDetail`      | Single event details, booking action, agenda                                |
| `LoginRegister`    | Combined login / registration form                                          |
| `UserDashboard`    | "My Bookings" — the current user's registrations                            |
| `AdminDashboard`   | Tabbed admin panel: manage **events**, manage **categories**, moderate **bookings** |

### Auth State

`src/context/AuthContext.jsx` provides `AuthProvider` and the `useAuth()` hook:

- Token is persisted in `localStorage` and set as the default axios `Authorization` header.
- The JWT is decoded client-side (`parseJwt`) to extract `id`, `email`, and `role`; expired tokens trigger `logout()`.
- Exposes `{ token, user, login, register, logout, isAuthenticated, loading }`.

### Styling & Theme

- **Bootstrap 5** is the base, layered with a custom **dark glassmorphism** theme.
- Theme tokens live in `src/App.css` under `:root` (e.g. `--primary-color: #6366f1`, `--card-bg`, `--text-primary`, `--border-color`, `--danger-color`, `--success-color`). Reuse these variables instead of hard-coded colors.
- The Admin Dashboard tables use the `.admin-table` glass panel (gradient header, zebra rows, compact circular action buttons) with a mobile "stacked card" layout driven by `data-label` attributes. Bootstrap's default white cell background is reset to transparent inside `.admin-table` so the dark panel shows through.

---

## Configuration

**Backend** — `EventBoard.Api/appsettings.json`:
```json
{
  "ConnectionStrings": { "DefaultConnection": "Data Source=EventBoard.db" },
  "Jwt": {
    "Key": "ThisIsADevelopmentSecretKeyThatIsAtLeast32BytesLong!",
    "Issuer": "EventBoard.Api",
    "Audience": "EventBoard.Client"
  }
}
```

**Frontend** — API base URL resolves from `import.meta.env.VITE_API_BASE_URL`, falling back to `/api` (which the Vite proxy forwards to the backend). To point at a different backend, set `VITE_API_BASE_URL` in a `.env` file inside `event-board-frontend/`.

---

## Development Notes

- **CORS**: the backend enables an `AllowFrontend` policy that allows any origin/method/header — fine for development, tighten before production.
- **HTTPS redirect**: `Program.cs` calls `app.UseHttpsRedirection()`; when running the frontend against plain `http://localhost:5000`, ensure requests hit the HTTP endpoint (the Vite proxy handles this).
- **Database**: `DbInitializer.Seed()` runs on every startup but only inserts data when tables are empty. Delete `EventBoard.db` (and the `-shm`/`-wal` files) to reset and re-seed.
- **JWT secret**: the key in `appsettings.json` is a development placeholder. Replace it with a securely stored secret for any non-local deployment.
- **`mock-api.js`** is a lightweight standalone Node server (port 5050) that only serves `GET /events`; it exists for frontend-only experimentation and is **not** a substitute for the real API (no auth, categories, or bookings).

---

_EventBoard — full-stack React + ASP.NET Core sample application._
