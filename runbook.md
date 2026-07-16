# Event Board — Production Incident Runbook

Operational runbook for the Event Board API (.NET 8 + SQL Server + React/nginx,
orchestrated by `docker-compose.yml`). Structured logs are shipped to **Seq**
(Serilog sink) and browsable at **http://localhost:5341**.

## Using this runbook

- **Dashboard:** http://localhost:5341 → **Events** tab. Set the time range
  (top-right, e.g. *Last 1h*) before running a query.
- **Query language:** Seq filters use property names directly, e.g.
  `StatusCode >= 400`, and `@Level`, `@Message`, `@MessageTemplate`, `@Exception`
  for built-ins. `like '%text%' ci` does a case-insensitive substring match.
- **Key properties emitted by the app** (from `UseSerilogRequestLogging` in
  [`Program.cs`](EventBoard.Api/Program.cs)): `RequestMethod`, `RequestPath`,
  `StatusCode`, `Elapsed` (ms). Domain events add their own, e.g. `EventId`,
  `UserId`, `Email`.
- **Container logs (fallback if Seq is down):** `docker compose logs -f api`.

Quick triage query — everything that failed in the last hour:

```
StatusCode >= 500 or @Level in ['Error','Fatal']
```

---

## Incident 1 — Misconfigured Environment Variable

### Description
The API fails to start, restarts in a loop, or returns `500`/`503` for every
request immediately after a deploy or config change. The frontend shows a generic
error and no data loads. Typically triggered by a missing or wrong environment
variable — e.g. a bad `ConnectionStrings__DefaultConnection`, an empty
`Jwt__Key`, or a wrong SQL password.

### Symptoms (Seq)
The app logs a fatal startup error, or the DB seed-retry loop in
[`Program.cs`](EventBoard.Api/Program.cs) fires repeatedly:

```
@Level in ['Error','Fatal']
```
```
@MessageTemplate like '%Database not ready%'
```
```
@Exception like '%SqlException%' or @Exception like '%login failed%' ci
```

- A **healthy** startup instead shows: `Database migrated and seeded successfully.`
  followed by `Now listening on: http://[::]:8080`. Absence of these = startup failed.
- An empty/short `Jwt__Key` surfaces as an `ArgumentOutOfRangeException` /
  `ArgumentNullException` at startup (the JWT key must be ≥ 32 bytes).
- If Seq shows **no** recent events at all, the process is crashing before logging
  is up — check `docker compose logs api` directly.

### Root Cause
An environment variable is missing, empty, or wrong. Config precedence:
`docker-compose.yml` env → root `.env` → `appsettings.json`. A typo'd key
(`__` vs `:`), an unquoted special character, or an unset `${VAR}` in `.env` are
the usual culprits.

### Step-by-step Resolution
1. **Confirm the failure and read the error.**
   ```bash
   docker compose ps                 # is 'api' restarting / unhealthy?
   docker compose logs --tail=80 api # find the FTL/ERROR line
   ```
2. **Inspect the effective environment** of the running container:
   ```bash
   docker compose exec api printenv | grep -E 'ConnectionStrings|Jwt|OpenWeather|Seq'
   ```
   Compare against [`docker-compose.yml`](docker-compose.yml) and `.env.example`.
   Look for empty values or an unresolved `${MSSQL_SA_PASSWORD}`.
3. **Fix the variable.**
   - Set the correct value in the root `.env` (see `.env.example`) or in the
     `api.environment` block of `docker-compose.yml`.
   - Example: ensure `JWT_KEY` is ≥ 32 chars and `MSSQL_SA_PASSWORD` matches the
     `sqlserver` service password.
4. **Recreate the API with the new config:**
   ```bash
   docker compose up -d api
   ```
5. **Verify** in Seq (range *Last 5m*):
   - `@MessageTemplate like '%seeded successfully%'` returns the startup event.
   - `StatusCode >= 500` returns nothing new.
   - Smoke test: `curl -s -o /dev/null -w "%{http_code}\n" http://localhost:8080/api/events` → `200`.

---

## Incident 2 — File Upload Failure

### Description
Organizers cannot attach an event image. The upload to
`POST /api/events/upload-image` fails with **`413 Payload Too Large`** (file
exceeds the size limit) or **`400 Bad Request`** (unsupported type/extension).

### Symptoms (Seq)
All failed uploads (each shows as an HTTP request event with a 4xx `StatusCode`):
```
RequestPath = '/api/events/upload-image' and StatusCode >= 400
```
Distinguish the two causes by status code:
```
RequestPath = '/api/events/upload-image' and StatusCode = 400
```
```
RequestPath = '/api/events/upload-image' and StatusCode = 413
```

- `StatusCode = 400` → the file failed the whitelist / empty-file check in
  [`EventsController.cs`](EventBoard.Api/Controllers/EventsController.cs)
  (allowed: `.jpg .jpeg .png .gif .webp`, and extension **and** content-type must
  match).
- `StatusCode = 413` → the body exceeded a size limit before/at the API.

> Note: the exact 400 reason ("Unsupported file type…", "Image must be 5 MB or
> smaller.") is returned in the HTTP **response body**, not currently written to a
> log event — Seq shows only the `400`. To see the precise reason, reproduce with
> the `curl` in step 5 (without `-o /dev/null`) and read the response. A small
> improvement is to add a `_logger.LogWarning` for each rejection reason so it is
> queryable in Seq directly.

### Root Cause
Two enforced limits, plus a proxy limit:
- **API:** `MaxImageBytes = 5 MB` — enforced by `[RequestSizeLimit(MaxImageBytes)]`
  and an explicit `file.Length` check in
  [`EventsController.cs`](EventBoard.Api/Controllers/EventsController.cs).
- **nginx:** `client_max_body_size 10m` in
  [`nginx.conf`](event-board-frontend/nginx.conf) — a body over 10 MB is rejected
  by the proxy with `413` before it reaches the API (so it won't appear in the API
  logs — check `docker compose logs frontend`).
- **Type whitelist:** extension **and** content-type must both be an allowed image.

### Step-by-step Resolution
1. **Identify which limit tripped** using the Seq queries above (400 = type,
   413 = size). For a 413 with nothing in the API log, check the proxy:
   ```bash
   docker compose logs --tail=50 frontend | grep -i 'client intended to send too large'
   ```
2. **If it's a user error** (unsupported/oversized file): tell the user the limits
   — image types only (`.jpg/.jpeg/.png/.gif/.webp`), ≤ 5 MB. No change needed.
3. **If the limit is genuinely too low**, raise it consistently in *both* places,
   otherwise nginx and the API disagree:
   - API: bump `MaxImageBytes` in `EventsController.cs`.
   - nginx: bump `client_max_body_size` in `event-board-frontend/nginx.conf`
     to match or exceed it.
   - To add a new image type, extend both `AllowedImageExtensions` and
     `AllowedImageContentTypes`.
4. **Rebuild the affected image(s):**
   ```bash
   docker compose up --build -d api frontend
   ```
5. **Verify:**
   ```bash
   # valid small image should return {"imageUrl":"/uploads/..."} and 200
   curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer <token>" \
     -F "file=@/path/to/photo.png;type=image/png" \
     http://localhost:8080/api/events/upload-image
   ```
   In Seq: `RequestPath = '/api/events/upload-image' and StatusCode = 200`.

---

## Incident 3 — Slow Endpoint (inefficient database query)

### Description
A page or API call is sluggish — e.g. the events list or an admin report takes
seconds to load. The endpoint still returns `200`, but latency is high, usually
from an inefficient query (an N+1 access pattern, a missing eager-load/`Include`,
or a missing index).

### Symptoms (Seq)
Every request's duration is logged as `Elapsed` (ms). Find the slow ones:
```
Elapsed > 1000
```
Narrow to a suspect endpoint and see the trend:
```
RequestPath like '/api/events%' and Elapsed > 500
```
Useful views in Seq:
- Sort the result list by `Elapsed` descending to find the worst offenders.
- Chart it: run `select count(*), mean(Elapsed) from stream where RequestPath =
  '/api/events' group by time(1m)` in the query bar to see latency over time.
- **To see the SQL** EF Core emits (to spot an N+1), the EF log level must be
  raised — it is capped at `Warning` by default in
  [`appsettings.json`](EventBoard.Api/appsettings.json) (`Serilog.MinimumLevel.
  Override."Microsoft.EntityFrameworkCore"`). Temporarily set it to `Information`
  and restart `api`; repeated near-identical `SELECT` events for a single request
  then indicate an **N+1** pattern. Revert afterwards (EF `Information` is noisy).

### Root Cause
Typically one of:
- **N+1 queries** — iterating a collection and lazily loading a related entity
  per item instead of eager-loading with `.Include()`.
- **Missing `Include` / projection** — related data fetched in a follow-up round
  trip, or whole entities loaded when a `.Select()` projection would do.
- **Missing index** — a filter/sort on an unindexed column (e.g. searching or
  ordering `Events` by a non-key column) forces a table scan.
- **Tracking overhead** — read-only queries not using `.AsNoTracking()`.

Repository queries live in [`EventBoard.Api/Repositories/`](EventBoard.Api/Repositories);
the existing `GetAllAsync` in `EventRepository.cs` already eager-loads
`Category`/`Organizer`/`Bookings` with `.AsNoTracking()` — follow that pattern.

### Step-by-step Resolution
1. **Pinpoint the endpoint** from the `Elapsed > 1000` query — note its
   `RequestPath` and typical `Elapsed`.
2. **Reproduce and confirm** the slow path:
   ```bash
   curl -s -o /dev/null -w "%{time_total}s\n" http://localhost:8080/api/events
   ```
3. **Find the cause.** Temporarily inspect the SQL EF Core emits (EF command
   logging is already at Debug/Information) for that request in Seq — many
   repeated `SELECT`s = N+1; one big scan = missing index.
4. **Fix in the repository query:**
   - Add `.Include(...)` (and `.ThenInclude(...)`) to eager-load related data in
     one round trip; add `.AsNoTracking()` for read-only lists.
   - Replace "load entity then map" with a `.Select(... => new Dto {...})`
     projection so only needed columns are fetched.
   - For a missing index, add one via an EF Core migration:
     ```bash
     dotnet ef migrations add Index_Events_<Column> -p EventBoard.Api
     docker compose up --build -d api   # migrations apply on startup
     ```
5. **Verify the improvement:**
   - Re-run the `curl` timing above — latency should drop substantially.
   - In Seq (range *Last 15m*): the same `RequestPath` now shows `Elapsed` well
     under the previous value; `Elapsed > 1000` no longer returns it.
   - `dotnet test EventBoard.Api.Tests` still passes (no behavior change).

---

## Appendix — Seq setup & health

- **Service:** `seq` in [`docker-compose.yml`](docker-compose.yml)
  (`datalust/seq`, `5341:80`, `seq_data` volume, auth disabled for local dev via
  `SEQ_FIRSTRUN_NOAUTHENTICATION=true`).
- **Shipping:** Serilog Console + Seq sinks configured in
  [`Program.cs`](EventBoard.Api/Program.cs); the API sends to `http://seq:80`
  over the compose network (`Seq__ServerUrl`), while operators use
  `http://localhost:5341`.
- **Is Seq receiving?** `curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5341`
  → `200`. If the dashboard is empty, confirm the `api` and `seq` containers are
  both up (`docker compose ps`) and that `api` logs no Seq connection warnings.
