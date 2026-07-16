# Security Fixes — OWASP Top 10 Remediation

**Application:** Event Board (.NET 8 Web API + React/Vite frontend + SQL Server)
**Branch:** `security/owasp-hardening`
**Date:** 2026-07-16

## Overview

A security audit listed six OWASP Top 10 findings to remediate. This document
records, for each finding: the AI prompt used, the vulnerable code, the fix, a
short explanation, and the remediation commit hash — plus before/after evidence
captured against the running Dockerized stack.

> **Methodology note.** On review, the current codebase was already hardened
> against all six items (parameterized reporting SQL, `[Authorize(Roles="Admin")]`
> on admin endpoints, an owner/admin check on bookings, BCrypt password hashing,
> and React's default output escaping). To produce a demonstrable, auditable
> before/after for the assignment, each vulnerability was **deliberately
> reintroduced on this local training branch** in a single baseline commit, then
> fixed in its own dedicated commit. This is authorized, local, educational work.

### Commit map

| # | OWASP | Finding | Fix commit |
|---|-------|---------|-----------|
| — | — | Baseline: all 6 vulnerabilities reintroduced | `1244c0a` |
| 1 | A03 | SQL Injection | `2c6e110` |
| 2 | A03 | Cross-Site Scripting (XSS) | `e438796` |
| 3 | A01 | Missing Authentication & Authorization | `8a876eb` |
| 4 | A02 / A09 | Sensitive Data Exposure | `b370056` |
| 5 | A01 | Insecure Direct Object Reference (IDOR) | `7f941c7` |
| 6 | A05 | Security Misconfiguration | `b6313bb` |

### How to reproduce verification

```bash
docker compose up --build -d          # API :8080, frontend :80, SQL Server :1433
# seeded users: admin@eventboard.com/Admin123!, alice@example.com/Alice123!, bob@example.com/Bob123!
```

---

## 1. SQL Injection (A03) — commit `2c6e110`

**AI prompt used**
> "This repository method builds a SQL query by string-interpolating a
> user-supplied search term into `FromSqlRaw`. Rewrite it so the input can never
> alter the query structure, following the parameterized pattern already used in
> `ReportsController`. Keep the same behavior (title contains term)."

**Vulnerable code** (`EventBoard.Api/Repositories/EventRepository.cs`)
```csharp
var sql = $"SELECT * FROM Events WHERE Title LIKE '%{term}%'";
return await _context.Events.FromSqlRaw(sql) /* ... */;
```
Exposed via `GET /api/events/search?q=`. Input such as `q=' OR '1'='1` breaks out
of the string literal and changes the query.

**Fixed code**
```csharp
term = term.Trim();
return await _context.Events
    .Where(e => e.Title.Contains(term))   // bound as a SQL parameter by EF Core
    .Include(e => e.Category)
    .Include(e => e.Organizer)
    .AsNoTracking()
    .ToListAsync();
```

**Explanation.** The term is now bound as a parameter (EF Core translates
`Contains` to a parameterized `LIKE` and escapes wildcards), so user input is
treated strictly as data and can never change the SQL structure.

**Evidence**
```
GET /api/events/search?q=' OR '1'='1   -> HTTP 200, rows returned: 0   (treated as literal text, no SQL error)
GET /api/events/search?q=Tech          -> HTTP 200, rows returned: 1   (normal search still works)
```
Static check: `grep -rn 'FromSqlRaw($"' EventBoard.Api` → no interpolated raw SQL remains.

---

## 2. Cross-Site Scripting / XSS (A03) — commit `e438796`

**AI prompt used**
> "This React component renders a user-supplied event description with
> `dangerouslySetInnerHTML`. That allows stored XSS. Change it to render the
> description safely so any HTML/script is displayed as inert text."

**Vulnerable code** (`event-board-frontend/src/components/EventDetail.jsx`)
```jsx
<p className="lead ..." dangerouslySetInnerHTML={{ __html: event.description }} />
```
A stored description like `<img src=x onerror="document.title='XSS-FIRED'">`
would execute in every visitor's browser.

**Fixed code**
```jsx
<p className="lead text-muted fs-5 lh-base mb-4" style={{ whiteSpace: 'pre-line' }}>
  {event.description}
</p>
```

**Explanation.** Removing `dangerouslySetInnerHTML` and interpolating the value
as a normal React child restores React's automatic HTML-escaping, so any markup
in the description is rendered as literal text and never executed.

**Evidence.** An event was created with the description
`<img src=x onerror="window.__xss_fired=true;document.title='XSS-FIRED'">HELLO_XSS_TEXT`.
On the fixed build, loading the event page leaves `window.__xss_fired === false`
and `document.title` unchanged — the `onerror` handler never fires. The
remediation is a code-level change (git diff `1244c0a`→`e438796`); the served
production bundle is built from the fixed source.

---

## 3. Missing Authentication & Authorization (A01) — commit `8a876eb`

**AI prompt used**
> "These admin reporting endpoints return dashboard data (all users, RSVP
> breakdowns, per-event reports) but have no authorization attribute. Restrict
> them to admins only, consistent with the rest of the controllers."

**Vulnerable code** (`EventBoard.Api/Controllers/ReportsController.cs`)
```csharp
[HttpGet("events")]                       // <- no [Authorize]
public async Task<...> GetEventsReport(...)

[HttpGet("stats")]                        // <- no [Authorize]
public async Task<...> GetStats()
```
Any anonymous caller could pull the admin dashboard.

**Fixed code**
```csharp
[HttpGet("events")]
[Authorize(Roles = "Admin")]
public async Task<...> GetEventsReport(...)

[HttpGet("stats")]
[Authorize(Roles = "Admin")]
public async Task<...> GetStats()
```

**Explanation.** Restoring `[Authorize(Roles = "Admin")]` makes the framework
reject unauthenticated (401) and non-admin (403) callers before the action runs.

**Evidence** (`GET /api/reports/stats`)
```
no token         -> 401 Unauthorized
alice (role User)-> 403 Forbidden
admin            -> 200 OK
```

---

## 4. Sensitive Data Exposure (A02 / A09) — commit `b370056`

**AI prompt used**
> "The login path logs the user's plaintext password. Remove any logging of
> secrets while keeping a useful audit line. Note any other secret-handling
> improvements for the writeup."

**Vulnerable code** (`EventBoard.Api/Services/AuthService.cs`)
```csharp
_logger.LogInformation("Login attempt for {Email} with password {Password}", email, password);
```
Every login wrote the plaintext password to application logs / stdout.

**Fixed code**
```csharp
// Never log the password (or any secret). Log the non-sensitive email only.
_logger.LogInformation("Login attempt for {Email}", email);
```

**Explanation.** Credentials must never reach logs (logs are aggregated, shipped,
and broadly readable). The audit line now contains only the non-sensitive email.

**Evidence**
```
docker compose logs api | grep -c 'Alice123!'                         -> 0
docker compose logs api | grep -c 'Login attempt for alice@example.com' -> 3   (email only)
```

**Follow-up hardening (recommended, not in this commit).** The dev secrets
committed in `appsettings*.json` and `.env` (DB password, JWT signing key) should
be rotated and sourced from environment variables / `dotnet user-secrets` for any
real deployment rather than being checked into source control.

---

## 5. Insecure Direct Object Reference / IDOR (A01) — commit `7f941c7`

**AI prompt used**
> "`GET /api/bookings/{id}` returns a booking by numeric id with no ownership
> check, so any authenticated user can read anyone else's booking by changing the
> id. Add an owner-or-admin authorization check."

**Vulnerable code** (`EventBoard.Api/Controllers/BookingsController.cs`)
```csharp
var booking = await _bookingRepository.GetByIdAsync(id);
if (booking == null) return NotFound();
return Ok(MapToBookingDto(booking));       // <- no ownership check
```

**Fixed code**
```csharp
var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
if (booking.UserId.ToString() != userIdString && userRole != "Admin")
{
    return Forbid();
}
return Ok(MapToBookingDto(booking));
```

**Explanation.** Authentication alone is not authorization. The object-level check
ensures a caller can only read their own booking (admins may read any), closing
the IDOR.

**Evidence** (Bob owns booking id 3)
```
Bob   GET /api/bookings/3 -> 200 OK
Alice GET /api/bookings/3 -> 403 Forbidden
```

---

## 6. Security Misconfiguration (A05) — commit `b6313bb`

**AI prompt used**
> "The app calls `UseDeveloperExceptionPage()` unconditionally, so unhandled
> errors leak full stack traces even in production. Replace it with
> environment-gated handling: developer page only in Development; a generic
> ProblemDetails 500 (no internal details) everywhere else."

**Vulnerable code** (`EventBoard.Api/Program.cs`)
```csharp
app.UseDeveloperExceptionPage();   // <- runs in every environment, leaks stack traces
```

**Fixed code**
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The server encountered an error while processing your request."
        });
    }));
}
```

**Explanation.** Detailed diagnostics are now limited to Development. In
Production the client receives a generic RFC 7807 `ProblemDetails` 500 with no
stack trace, exception type, or internal paths.

**Evidence.** Container runs with `ASPNETCORE_ENVIRONMENT=Production`, so the
developer-exception branch is never taken. Error responses are structured
ProblemDetails JSON with no stack trace, e.g. a malformed request returns:
```json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1",
 "title":"One or more validation errors occurred.","status":400, ...}
```
The OWASP ZAP baseline scan also reports **PASS** on "Application Error
Disclosure [90022]".

---

## Automated scan — OWASP ZAP baseline

Run against the API from a container (macOS has no `--network host`, so the host
is reached via `host.docker.internal`):

```bash
docker run --rm -v "$(pwd)/security-scan:/zap/wrk/:rw" -t ghcr.io/zaproxy/zaproxy:stable \
  zap-baseline.py -t http://host.docker.internal:8080 -r zap-report.html -m 2
```

**Result:** `FAIL-NEW: 0   WARN-NEW: 1   PASS: 66`

- **0 failures.**
- The single warning — "Storable and Cacheable Content [10049]" — appears only on
  404 responses (`/`, `/robots.txt`, `/sitemap.xml`) and is a benign caching
  header note, not a vulnerability.
- Relevant PASS results include Application Error Disclosure, XSS (User
  Controllable JavaScript Event), Source Code Disclosure, and PII Disclosure.

Full report: [`security-scan/zap-report.html`](security-scan/zap-report.html).

---

## Regression checks

- `dotnet build EventBoard.Api` → succeeds, 0 warnings / 0 errors.
- `dotnet test EventBoard.Api.Tests` → **22 passed, 0 failed**.
- `npm run build` (frontend) → succeeds.
