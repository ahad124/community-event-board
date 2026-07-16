# AI Prompts Log — Phase 3 Live-Site Incident Simulation

A trace of the natural-language prompts used with the AI assistant to detect,
diagnose, and resolve each incident. Prompts are grouped by incident and ordered
detection → diagnosis → resolution → verification.

---

## Incident A — Misconfigured Environment Variable

1. **(Resilience / pre-step)**
   > "The startup seed/migrate loop in `Program.cs` rethrows on the final attempt,
   > which crashes the process when the database is unreachable. Make it non-fatal:
   > after the retries are exhausted, log an error and continue to `app.Run()` so a
   > misconfiguration surfaces as observable request-time errors rather than a boot
   > crash loop. Keep the existing retry/backoff for transient startup delays."

2. **(Detection)**
   > "Several API endpoints are returning 500. Which Seq query shows me the failing
   > requests and the underlying exception, and how do I tell whether it's a code
   > bug or a configuration problem?"
   >
   > Used query: `StatusCode >= 500 or @Level in ['Error','Fatal']`.

3. **(Diagnosis)**
   > "Seq shows `SqlException: Login failed for user 'sa'` on every DB-backed
   > endpoint while `/swagger` still returns 200. What does that pattern tell us
   > about the root cause?"

4. **(Resolution)**
   > "How do I confirm which environment variable is wrong in the running container
   > and safely roll back the connection string, then redeploy just the API service
   > with Docker Compose?"
   >
   > Used: `docker compose exec api printenv | grep ConnectionStrings`, restore the
   > value, `docker compose up -d api`.

5. **(Verification)**
   > "Give me commands to confirm the service has recovered — HTTP status of the
   > previously failing endpoints and a Seq query proving no new 500s."

---

## Incident B — Slow Endpoint (N+1 Query)

1. **(Detection)**
   > "Users report the events listing page times out. Using the Serilog request
   > timing already flowing to Seq, which query finds the slow endpoint and its
   > response time?"
   >
   > Used query: `Elapsed > 1000` (sorted by `Elapsed` desc), which surfaced
   > `RequestPath = '/api/events/detailed'`.

2. **(Diagnosis)**
   > "How do I turn on EF Core command logging temporarily to see the SQL for one
   > request, and what does the output look like for an N+1 query?"
   >
   > Enabled `Microsoft.EntityFrameworkCore.Database.Command = Information`,
   > observed 94 `SELECT FROM [Bookings]` + 94 `SELECT FROM [Favorites]` for a
   > single request (206 DB commands) — the N+1 signature.

3. **(Optimization)**
   > "This repository method loads all events, filters in memory, then queries each
   > event's category/organizer/bookings/favorites separately — an N+1. Rewrite it
   > as a single EF Core query using `AsNoTracking` and a `Select` projection that
   > computes the RSVP tallies and favorites count in SQL, applying the filters in
   > the query so the Date/CategoryId/Location indexes are used. Keep the DTO output
   > identical."

4. **(Index)**
   > "The `location` filter has no supporting index and `Location` is `nvarchar(max)`
   > which SQL Server can't index. Cap its length and add an index, then generate
   > the EF Core migration."

5. **(Verification)**
   > "Give me commands to measure the endpoint's response time (cold and warm) and
   > confirm it is under 500 ms, verify the JSON output is unchanged, and run the
   > test suite to check for regressions."

---

## Notes

- All prompts were used against the app's own source and the local Docker stack;
  no code from external sources was introduced.
- Fixes were verified end-to-end (HTTP status, Seq `Elapsed`/`@Level`, `curl`
  timings, and `dotnet test`) before being committed — see
  [`postmortem-incident-a.md`](postmortem-incident-a.md) and
  [`postmortem-incident-b.md`](postmortem-incident-b.md).
