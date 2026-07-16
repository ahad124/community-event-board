# Post-Mortem — Incident A: Misconfigured Environment Variable

- **Title:** API-wide HTTP 500s from an invalid database connection string
- **Date:** 2026-07-16
- **Incident Commander:** ahad124
- **Severity:** SEV-1 (all data-backed endpoints failing)
- **Status:** Resolved

## Summary

A configuration change deployed the API with an **invalid database password** in
`ConnectionStrings__DefaultConnection`. The application process started and served
traffic, but every database-backed endpoint (`/api/events`, `/api/categories`,
`/api/auth/login`, …) returned **HTTP 500**. Static endpoints that don't touch the
database (e.g. `/swagger`) continued to return 200. Users could not log in or view
events. The fault was detected in Seq, root-caused to the connection string, and
resolved by restoring the correct credential and redeploying the API container.
Total user-facing impact: ~6 minutes.

## Timeline (UTC)

| Time | Event |
|------|-------|
| 12:54:11 | Misconfiguration deployed — API redeployed with `Password=WRONG_PASSWORD…` in the connection string. |
| 12:54–12:57 | API retries DB init 3× (per startup policy), then boots anyway (resilience behaviour) and begins serving request-time 500s. |
| 12:57:31 | First confirmed `500` on `GET /api/events`. Detection begins. |
| 12:58 | Seq query `@Level = 'Error'` surfaces `SqlException: Login failed for user 'sa'` and `HTTP GET /api/events responded 500`. |
| 12:59 | Diagnosis: all DB endpoints 500 while process is up + "Login failed for user 'sa'" ⇒ credential/connection-string misconfiguration, not a code defect. |
| 13:00:48 | Fix applied — correct connection string restored; `docker compose up -d api`. |
| 13:00:55 | Recovery confirmed — `GET /api/events` → 200, `POST /api/auth/login` → 200. |

## Detection

Seq (`http://localhost:5341`) with:

```
StatusCode >= 500 or @Level in ['Error','Fatal']
```

returned, among others:

```
[Error] An error occurred using the connection to database 'EventBoardDb' on server 'sqlserver,1433'
[Error] HTTP GET /api/events responded 500 in 628.2098 ms
[Error] An unhandled exception has occurred while executing the request.
        Microsoft.Data.SqlClient.SqlException (0x80131904): Login failed for user 'sa'.
```

Endpoint probe during the incident:

```
GET /api/events         -> 500
GET /api/categories     -> 500
POST /api/auth/login     -> 500
GET /swagger/index.html -> 200   (no DB dependency)
```

Clients received a generic RFC 7807 problem document (no stack trace leaked — the
Phase 1 hardening held):

```json
{"title":"An unexpected error occurred.","status":500,
 "detail":"The server encountered an error while processing your request."}
```

## Root Cause

The `ConnectionStrings__DefaultConnection` environment variable for the `api`
service contained an **incorrect SQL Server password**. SQL Server rejected every
connection with error 18456 ("Login failed for user 'sa'"), so all EF Core queries
threw at request time. Because the app had already started (the startup DB-init
step is non-fatal), the failure surfaced as per-request 500s rather than a boot
crash.

Contributing factor: the DB credential is supplied via configuration/environment,
and there was no startup validation or health gate to reject an unusable database
connection before the container began accepting traffic.

## Resolution and Recovery

1. Identified the offending variable by comparing the running container's
   environment against source of truth:
   `docker compose exec api printenv | grep ConnectionStrings`.
2. Restored the correct connection string (revert the config change).
3. Redeployed: `docker compose up -d api`.
4. Verified recovery in Seq (`StatusCode >= 500` returns nothing new) and by
   endpoint probes (`/api/events` and `/api/auth/login` both 200).

## Lessons Learned & Preventive Measures

**What went well**
- Structured logs in Seq made detection and root-cause near-instant — the exact
  `SqlException` message pointed straight at credentials.
- The Phase 1 global exception handler prevented stack-trace leakage even while
  every request was failing.

**What to improve / action items**
1. **Add a startup connectivity check + readiness probe** so a container with an
   unusable DB connection is marked unhealthy and (in real orchestration) not sent
   traffic, instead of serving 500s. *(owner: platform)*
2. **Validate critical configuration on boot** (fail-fast with a clear log if the
   DB is unreachable *and* the operator opts into strict mode) — balance against
   the resilience requirement of surviving transient DB blips. *(owner: backend)*
3. **Manage secrets outside the compose file** (env/secret store) and add a smoke
   test to the deploy pipeline that hits `/api/events` and asserts 200 before
   completing the rollout. *(owner: devops)*
4. **Alert** on `StatusCode >= 500` rate in Seq so detection is automatic rather
   than manual. *(owner: platform)*

## AI Prompts Used

See [`ai-prompts-log.md`](ai-prompts-log.md) (Incident A section). Key prompt that
drove the resilience change:

> "The startup seed/migrate loop rethrows on the final attempt, which crashes the
> process when the DB is unreachable. Make it non-fatal: after retries are
> exhausted, log an error and continue to `app.Run()` so a misconfiguration
> surfaces as observable request-time errors instead of a boot crash loop."
