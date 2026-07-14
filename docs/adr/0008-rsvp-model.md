# 8. Model RSVPs as yes / maybe / no with upsert

Date: 2026-07-14

## Status

Accepted

## Context

The brief requires users to RSVP **yes / maybe / no** to an event, with the RSVP count
shown on the detail page. An earlier prototype modeled this as a "booking" with a
`Pending / Confirmed / Cancelled` status that only an admin could change — which does
not match RSVP semantics.

## Decision

- The `BookingStatus` enum represents the RSVP response: `Yes`, `Maybe`, `No`
  (persisted as its string name).
- `POST /api/bookings` is an **upsert**: it creates the caller's RSVP or updates the
  existing one, so a user can change their response freely.
- The RSVP owner is taken from the JWT.
- `EventDto` exposes per-status tallies (`RsvpYesCount/MaybeCount/NoCount/TotalCount`),
  computed by including bookings in the event query — so the detail page and dashboards
  can show counts without extra round trips.
- Admins view RSVPs read-only; there is no "moderation" of another user's RSVP.

The `EventBooking` entity name is retained internally to limit churn; externally the
feature is presented as "RSVP".

## Consequences

- Behavior matches the brief; responses are user-owned and changeable.
- Including bookings to compute counts is acceptable at this scale; a very large data
  set would warrant a projected aggregate query instead.
