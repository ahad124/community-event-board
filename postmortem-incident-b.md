# Post-Mortem — Incident B: Slow Endpoint (N+1 Query)

- **Title:** `/api/events/detailed` exceeds 3 s and times out under load
- **Date:** 2026-07-16
- **Incident Commander:** ahad124
- **Severity:** SEV-2 (feature slow/unusable; no data loss)
- **Status:** Resolved

## Summary

A newly shipped "detailed events" listing (`GET /api/events/detailed`, with
filtering) was **severely slow** — ~2.5–3.5 s on a 1,000-event dataset, with users
reporting timeouts on the listing page. The endpoint used an **N+1 query pattern**:
it loaded all events, filtered them in memory, then issued separate database
queries per event for category, organizer, bookings, and favorites. It was
identified via Serilog request-timing in Seq, confirmed with EF Core command logs,
and fixed by rewriting the data access as a single set-based, projected query
(plus a supporting index). Response time dropped to **<200 ms** (>15× faster).

## Timeline

| Phase | Event |
|-------|-------|
| Detection | Seq `Elapsed > 1000` surfaced `RequestPath = '/api/events/detailed'` at ~2534–2623 ms while every other endpoint stayed <400 ms. |
| Reproduction | `curl -w "%{time_total}"` on `/api/events/detailed`: 3.55 s (cold), ~2.5 s (warm). Filtered `?location=Online` was faster (fewer rows) — a hallmark of per-row query cost. |
| Diagnosis | Temporarily raised EF Core command logging to `Information`; a single filtered call (~94 events) issued **94 `SELECT … FROM [Bookings]` + 94 `SELECT … FROM [Favorites]`** (206 DB commands total) — a classic N+1. |
| Fix | Rewrote `EventRepository.GetDetailedAsync` as one `AsNoTracking` projection with SQL-side aggregates; added an `Events.Location` index. |
| Verification | `/api/events/detailed` now 45–199 ms unfiltered, ~100 ms filtered; Seq `Elapsed` 65–100 ms. 22/22 tests pass. |

## Detection

Request timing is emitted by `UseSerilogRequestLogging` (Phase 2) — no extra
middleware was needed. In Seq:

```
Elapsed > 1000
```

isolated the slow path:

```
HTTP GET /api/events/detailed responded 200 in 2534.2 ms   (Elapsed=2534, StatusCode=200)
HTTP GET /api/events/detailed responded 200 in 2623.4 ms
```

## Root Cause

`GetDetailedAsync` performed application-side work that should have been done by
the database:

1. `await _context.Events.ToListAsync()` loaded **every** row, then filtered
   in memory (ignoring the `Date`/`CategoryId` indexes).
2. For **each** surviving event it ran separate round trips:
   `Categories.FindAsync`, `Users.FindAsync`, `Bookings.Where(...).ToListAsync`,
   `Favorites.Where(...).CountAsync`.

With N events this is O(N) queries (dominated by the un-cached `Bookings` and
`Favorites` lookups — 2 per event). At 1,000 events that is ~2,000+ sequential
round trips, so latency grew linearly with the dataset and blew past 3 s.

**EF Core log evidence** (one `?location=Online` request, ~94 matching rows):

```
  94  FROM [Favorites]
  94  FROM [Bookings]
   6  FROM [Categories]     (deduped via EF identity map / FindAsync cache)
   4  FROM [Events]
   3  FROM [Users]          (deduped)
—— 206 total "Executed DbCommand" for a single HTTP request ——
```

## Resolution and Recovery

Rewrote the repository method as a **single projected query**
([`EventRepository.cs`](EventBoard.Api/Repositories/EventRepository.cs)):

- `.AsNoTracking()` (read-only; no change-tracking overhead).
- Filters applied in the query (`Where`) so SQL Server uses the
  `Date` / `CategoryId` / new `Location` indexes.
- Per-event aggregates computed **in SQL** via navigation aggregates in a
  `.Select(...)` projection:
  `e.Bookings.Count(b => b.Status == Yes)`, `e.Favorites.Count`,
  `e.Category.Name`, `e.Organizer.Email`.
- Added `HasIndex(e => e.Location)` (with `Location` capped to `nvarchar(256)` so
  it is indexable) via migration `Index_Events_Location` to support the location
  filter.

**Before/after (1,000-event dataset):**

| Call | Before (N+1) | After (single query) |
|------|--------------|----------------------|
| `/api/events/detailed` (unfiltered) | ~2.5–3.5 s | 45–199 ms |
| `/api/events/detailed?location=Online` | ~1.4 s (94 rows) | ~100 ms |
| DB commands per request | O(N) (hundreds–thousands) | 1 |

Output was verified identical in shape and values (RSVP tallies and favorites
counts match the seeded data). All 22 unit tests pass.

## Lessons Learned & Preventive Measures

**What went well**
- Phase 2 request-timing made the slow endpoint obvious in seconds via a single
  Seq query (`Elapsed > 1000`).
- EF Core command logs gave an unambiguous N+1 signature.

**What to improve / action items**
1. **Code review guardrail:** flag `await`-in-a-loop over `_context` and
   in-memory filtering of full tables; prefer projections + `Include`. *(owner: backend)*
2. **Performance budget in CI:** add a load test asserting list endpoints stay
   <500 ms on a representative dataset, so regressions are caught pre-merge.
   *(owner: devops)*
3. **Standing Seq alert** on `Elapsed > 1000` grouped by `RequestPath`. *(owner: platform)*
4. **Index review** when adding filterable columns (the `Location` filter had no
   supporting index). *(owner: backend)*

## AI Prompts Used

See [`ai-prompts-log.md`](ai-prompts-log.md) (Incident B section). Key prompt that
drove the fix:

> "This repository method loads all events, filters in memory, then queries each
> event's category/organizer/bookings/favorites separately — an N+1. Rewrite it as
> a single EF Core query using `AsNoTracking` and a `Select` projection that
> computes the RSVP tallies and favorites count in SQL, applying the filters in the
> query so the Date/CategoryId/Location indexes are used. Keep the DTO output
> identical."
