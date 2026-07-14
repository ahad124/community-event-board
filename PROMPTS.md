# AI Prompt Log

The 20 most impactful prompts used while building the Community Event Board with an
AI pair-programmer, in roughly the order they were used. Each entry notes the intent
and what it produced.

---

### 1. Project scaffolding
> "Scaffold an ASP.NET Core 8 Web API called EventBoard.Api using controllers, EF Core,
> and a controller → service → repository layering. Add entities for User, Event,
> Category, EventBooking and EventFavorite with sensible relationships."

Produced the initial solution structure, entities, `AppDbContext`, and DI wiring.

### 2. JWT authentication
> "Add JWT bearer authentication with BCrypt password hashing. Issue tokens containing
> the user id, email and role claim, and add register/login endpoints."

Generated `AuthService`, `JwtTokenService`, `AuthController`, and JWT setup in `Program.cs`.

### 3. Role-based authorization
> "Protect create/update/delete endpoints and add an Admin-only policy using
> `[Authorize(Roles = \"Admin\")]`. Seed an admin account admin@eventboard.com / Admin123!."

Added authorization attributes and `DbInitializer` seeding.

### 4. Event CRUD + DTOs
> "Implement full CRUD for events returning DTOs (not entities), with validation and
> Swagger response types."

Produced `EventsController`, `EventDto`, request DTOs, and repository methods.

### 5. Local image upload
> "Add an endpoint to upload an event image to wwwroot/uploads. Validate extension and
> content-type against an allow-list, cap at 5 MB, and save with a random GUID filename."

Generated the hardened `UploadImage` action and static file serving.

### 6. Weather integration with resilience
> "Integrate OpenWeatherMap to fetch current conditions for an event's city. Use a typed
> HttpClient with a Polly retry policy, read the API key from configuration, and never
> throw to the caller — return an 'unavailable' state instead."

Produced `WeatherService`, `WeatherController`, and the Polly configuration.

### 7. React + Vite frontend scaffold
> "Create a client-side-rendered React 19 + Vite frontend with React Router, an
> AuthContext that stores the JWT in localStorage and decodes it, and axios with the
> Authorization header set globally."

Generated the SPA shell, `AuthContext`, routing and protected routes.

### 8. Event list & detail UI
> "Build an event list grid with category filtering and an event detail page that shows
> the weather panel, styled with Bootstrap 5."

Produced `EventList.jsx` and `EventDetail.jsx`.

### 9. Unit tests for services
> "Generate xUnit unit tests for AuthService and EventService using Moq, covering success,
> invalid input, and exception scenarios, following Arrange-Act-Assert."

Produced the service test suites.

### 10. Integration tests
> "Add integration tests using WebApplicationFactory with an in-memory EF Core database,
> asserting unauthorized access returns 401 for protected event endpoints."

Produced `CustomWebApplicationFactory` and `EventsControllerIntegrationTests`.

### 11. Gap analysis against the brief
> "Review this project against the Community Event Board brief and list what's implemented,
> what needs updating, and what's missing (Docker, SQL Server, RSVP semantics, admin user
> management, ADRs)."

Produced the roadmap that drove the remaining work.

### 12. Migrate SQLite → SQL Server
> "Replace the SQLite provider with SQL Server, move the connection string to configuration/
> environment, and generate an InitialCreate EF Core migration. Apply migrations on startup."

Swapped packages, updated `Program.cs`, added migrations, changed `DbInitializer`.

### 13. Provider-aware seeding
> "The integration tests use the InMemory provider which can't run migrations. Make the
> seeder call Migrate() for relational providers and EnsureCreated() otherwise."

Produced the `Database.IsRelational()` branch.

### 14. Dockerfile for the API
> "Write a multi-stage Dockerfile for the .NET 8 API that restores, publishes in Release,
> and runs on the aspnet:8.0 runtime image listening on 8080."

Produced `EventBoard.Api/Dockerfile`.

### 15. Dockerfile + Nginx for the frontend
> "Write a Dockerfile that builds the Vite app and serves it with Nginx, plus an nginx.conf
> that does SPA fallback and reverse-proxies /api and /uploads to the api service."

Produced the frontend `Dockerfile` and `nginx.conf`.

### 16. docker-compose for the full stack
> "Create a docker-compose.yml with SQL Server 2022 (healthcheck + volume), the API
> (depends on SQL Server healthy, uploads volume, env-driven config), and the Nginx
> frontend on http://localhost. Make it survive `docker-compose down -v` then `up --build`."

Produced `docker-compose.yml`, `.env.example` and `.dockerignore` files.

### 17. RSVP semantics
> "Change the booking model to RSVP yes/maybe/no. Make POST /bookings an upsert owned by
> the JWT user, expose RSVP tallies on EventDto, and show yes/maybe/no buttons plus live
> counts on the event detail page."

Refactored the enum, `BookingsController`, `EventDto`, reports, seed and `EventDetail.jsx`.

### 18. Open event creation + public browsing
> "Allow any authenticated user to create events and upload images (organizer from the JWT),
> make the event list and detail public, and add a Create Event page and 'My Created Events'
> to the dashboard."

Adjusted authorization, added `GET /events/mine`, `CreateEvent.jsx`, and route changes.

### 19. Admin user management
> "Add an IsActive field to User with a migration, an Admin-only UsersController to list users,
> promote/demote, and enable/disable accounts (blocking self-demotion/disable), reject disabled
> users at login, and a Manage Users tab in the admin dashboard."

Produced `UsersController`, the migration, login change, and the admin UI tab.

### 20. Statistics + docs
> "Add a GET /reports/stats endpoint (users, events, categories, RSVP breakdown, favorites)
> with a stats banner in the admin dashboard, then write the README, ADRs, and update the
> UAT plan for the new features."

Produced the stats endpoint/UI and the documentation set.
