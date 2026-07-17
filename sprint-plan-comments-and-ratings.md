# Sprint Plan — Event Comments & Ratings

- **Product:** Community Event Board
- **Sprint:** 24 (2-week sprint) · **Dates:** 2026-07-20 → 2026-07-31
- **Sprint length:** 10 working days
- **Prepared:** 2026-07-16

> Drafted with AI assistance, then customised to this codebase (Controller →
> Service → Repository layering, EF Core + migrations, JWT role-based auth, React +
> Vite SPA, Serilog/Seq observability).

---

## 1. Sprint goal

> Let attendees share feedback on events — post comments and give a 1–5 star
> rating — and surface an average rating across the board so users can gauge event
> quality at a glance, with admin moderation to keep content healthy.

This deepens engagement on top of the existing RSVP/favorite model and gives
organizers and admins signal on which events land well.

## 2. Success metrics

- ≥ 30% of users who RSVP to a past event leave a rating within 2 weeks of launch.
- Average rating is visible on 100% of event cards and detail pages.
- p95 latency of the event-list and detail endpoints stays **< 500 ms** with
  ratings aggregated (no regression vs. the Phase 3 optimised listing).
- Zero unmoderated abusive comments outstanding > 24 h (admin delete available).

## 3. Scope

**In scope**
- Post / edit / delete **your own** comment on an event (authenticated users).
- Give / update **your own** 1–5 star rating on an event (one rating per user per
  event; upsert — mirrors the RSVP model in [ADR 0008](ADRs/0008-rsvp-model.md)).
- Show **average rating + rating count** on the event board and detail page.
- Show a comment list (most recent first, paginated) on the event detail page.
- **Admin moderation:** delete any comment; endpoints role-gated.
- Structured Serilog events for new actions (comment created/deleted, rating set).

**Out of scope (future sprints)**
- Threaded replies / nested comments.
- Emoji reactions or comment likes.
- Notifications when someone comments (depends on a future notifications feature).
- Profanity auto-filtering / ML moderation (manual admin delete only this sprint).
- Editing history / soft-delete audit trail.

## 4. User stories & acceptance criteria

Story point scale: Fibonacci (1, 2, 3, 5, 8). Total committed: **34 pts**.

### Attendee

**US-1 — Post a comment (3 pts)**
> As a signed-in user, I can post a comment on an event so I can share my thoughts.
- **Given** I am authenticated and viewing an event, **when** I submit a non-empty
  comment (≤ 1000 chars), **then** it is saved with my identity and appears at the
  top of the list without a full page reload.
- **Given** I am not authenticated, **when** I view the comment box, **then** I am
  prompted to sign in and cannot submit.
- Empty/whitespace-only or > 1000 char comments are rejected with a validation
  message (client and server).

**US-2 — Edit / delete my comment (3 pts)**
> As the author, I can edit or delete my own comment.
- I can edit/delete **only** comments I authored; the API returns **403** otherwise.
- Deleting removes it from the list immediately.

**US-3 — Rate an event (5 pts)**
> As a signed-in user, I can give an event a 1–5 star rating, and change it later.
- Selecting a star (1–5) saves my rating; re-selecting updates it (upsert — one
  row per (user, event)).
- Rating requires authentication; value outside 1–5 is rejected (**400**).
- After I rate, the displayed average and count update.

**US-4 — See ratings & comments while browsing (5 pts)**
> As any visitor, I can see an event's average rating and read its comments.
- Each event **card** shows `★ average (count)`; events with no ratings show
  "No ratings yet".
- The **detail page** shows the average, the count, and a paginated comment list
  (newest first, page size 20).

### Organizer

**US-5 — See feedback on my events (2 pts)**
> As an organizer, I can see the average rating and comments on events I organise
> (reusing the existing "my events" view).
- My events list shows each event's average rating and comment count.

### Admin

**US-6 — Moderate comments (5 pts)**
> As an admin, I can delete any comment to remove abuse.
- `DELETE` on any comment succeeds for `Admin`; non-admins deleting others'
  comments get **403**.
- Deletions are logged to Seq with the acting admin and target comment id.

**US-7 — Rating breakdown on admin reports (3 pts)**
> As an admin, the stats dashboard includes ratings coverage.
- The admin stats response includes total ratings and overall average; shown on
  the dashboard.

### Cross-cutting

**US-8 — Performance & observability (3 pts)**
> Average ratings must not slow the listing, and new actions must be observable.
- Average rating is computed in the **same projected query** as the detailed
  listing (no N+1); list/detail p95 stays < 500 ms on the 1000-event dataset.
- Comment/rating actions emit structured Serilog events visible in Seq.

**US-9 — Tests (2 pts)** — unit/integration coverage for the new services and
authorization rules (see task T-13).

## 5. Technical task breakdown

Mapped to the existing architecture. Representative files in parentheses.

| # | Task | Layer | Est |
|---|------|-------|----:|
| T-1 | `EventComment` + `EventRating` models (`Models/`) | Domain | 1 |
| T-2 | `AppDbContext` config: keys, FK relationships, indexes (`EventId`, `UserId`), **unique (UserId, EventId)** on ratings; `Comment` max length 1000 ([`AppDbContext.cs`](EventBoard.Api/Data/AppDbContext.cs), mirror the `NoAction` cascade note used for bookings/favorites) | Data | 2 |
| T-3 | EF migration `AddCommentsAndRatings` (`dotnet ef migrations add`) | Data | 1 |
| T-4 | `ICommentRepository` / `IRatingRepository` + implementations (mirror [`BookingRepository.cs`](EventBoard.Api/Repositories/BookingRepository.cs)) | Repo | 3 |
| T-5 | `ICommentService` / `IRatingService` (upsert rating; ownership checks) | Service | 3 |
| T-6 | `CommentsController`: GET (paged) / POST / PUT / DELETE; identity from JWT claims — reuse the `ClaimTypes.NameIdentifier ?? "sub"` pattern in [`BookingsController.cs`](EventBoard.Api/Controllers/BookingsController.cs); owner-or-admin on mutate | API | 3 |
| T-7 | `RatingsController`: POST/PUT upsert, GET my rating | API | 2 |
| T-8 | DTOs + validation: `CommentDto`, `CreateCommentRequest` (`[StringLength(1000, MinimumLength=1)]`), `RatingDto`, `SetRatingRequest` (`[Range(1,5)]`) — mirror DataAnnotations in [`AuthController.cs`](EventBoard.Api/Controllers/AuthController.cs) | API | 2 |
| T-9 | Add `AverageRating` + `RatingCount` (+ `CommentCount`) to the events projection; extend `EventDto`/`EventDetailedDto` and reuse the single-query projection pattern from `EventRepository.GetDetailedAsync` (no N+1) | API/Perf | 3 |
| T-10 | Admin stats: add `TotalRatings` / `AverageRating` to `StatsDto` ([`ReportsController.cs`](EventBoard.Api/Controllers/ReportsController.cs)) | API | 2 |
| T-11 | Frontend: `RatingStars` + `CommentList` / `CommentForm` components in event detail; show `★ avg (count)` on `EventList` cards (`event-board-frontend/src/components/`) | FE | 5 |
| T-12 | Frontend: wire to API (axios), optimistic update on post/rate, auth-gated controls (reuse `AuthContext`) | FE | 3 |
| T-13 | Tests: service upsert + ownership/authorization (403 paths); controller happy-path; extend `EventBoard.Api.Tests` (keep suite green) | QA | 2 |
| T-14 | Serilog events for comment/rating/moderation actions; verify they appear in Seq | Obs | 1 |
| T-15 | Docs: short ADR for the comments/ratings data model; update README feature list | Docs | 1 |

**Task points total: 34** (aligns with the story commitment).

## 6. Estimation & capacity

- **Team:** 2 backend, 1 frontend, 0.5 QA (shared).
- **Capacity:** ~34 ideal points for a 2-week sprint at this team's recent
  velocity (last 3 sprints: 31, 36, 33). Commitment of **34 pts** is in range.
- Backend-heavy early (models → migration → services → controllers), frontend
  picks up once the API contract is stable (~day 4).

## 7. Sprint schedule (milestones)

| Day | Milestone |
|-----|-----------|
| 1 | Kickoff; T-1–T-3 (models, DbContext, migration) done; API contract agreed with FE |
| 2–3 | T-4–T-7 repositories, services, controllers (comments + ratings) |
| 4 | T-8 DTOs/validation; **API contract frozen**; FE starts T-11 |
| 5–6 | T-9 average-rating projection; T-10 admin stats; FE T-11 components |
| 7 | T-12 FE wiring + optimistic updates; integration pass |
| 8 | T-13 tests; T-14 Seq events; bug-fix |
| 9 | T-15 docs; hardening; perf check (< 500 ms) |
| 10 | Sprint review + demo; retro |

## 8. Definition of Done

- All acceptance criteria met; feature reachable end-to-end in the Docker stack.
- New migration applies cleanly on a fresh DB (`docker compose up`); no data loss.
- `dotnet build` clean; `dotnet test` green (existing + new tests).
- Authorization verified: non-owner mutate → 403; admin moderation → 200; rating
  out of range → 400.
- List/detail p95 < 500 ms on the 1000-event dataset (single projected query — no
  N+1); confirmed via Seq `Elapsed`.
- New actions visible as structured events in Seq.
- ADR + README updated; PR reviewed and approved.

## 9. Risks & mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Average-rating aggregation reintroduces an N+1 / slows listing | Med | High | Compute in the same projected query as `GetDetailedAsync`; assert p95 in the perf check (US-8) |
| Rating uniqueness race (double submit) | Med | Med | DB unique index on (UserId, EventId) + upsert semantics; handle conflict gracefully |
| Comment abuse before moderation tooling matures | Med | Med | Admin delete from day one; server-side length/most-basic validation; log to Seq for visibility |
| Frontend blocked waiting on API | Med | Med | Freeze the API contract by day 4; FE mocks against the agreed DTOs until then |
| Cascade-delete complexity on SQL Server (multiple paths) | Low | Med | Reuse the `DeleteBehavior.NoAction` pattern already documented for bookings/favorites |

## 10. Dependencies

- Auth & roles ([ADR 0003](ADRs/0003-jwt-authentication-and-roles.md)) — reused as-is.
- EF Core + migrations ([ADR 0002](ADRs/0002-sql-server-with-ef-core-migrations.md)).
- Observability ([ADR 0010](ADRs/0010-structured-logging-with-serilog-and-seq.md)) —
  new events flow to Seq automatically.
- No new third-party services; no schema changes outside the two new tables.

## 11. Out-of-sprint backlog (candidate next)

- Notifications when someone comments on your event / organiser announcements.
- Threaded replies and reactions.
- Profanity filtering / automated moderation.
- "Most loved events" discovery feed driven by ratings.
