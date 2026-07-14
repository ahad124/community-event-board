# 3. JWT bearer authentication with role-based authorization

Date: 2026-07-14

## Status

Accepted

## Context

The API is consumed by a client-side-rendered SPA and must support stateless
authentication and an `Admin`-only area. We need password storage, token issuance,
and per-endpoint authorization.

## Decision

- Passwords are hashed with **BCrypt** (`BCrypt.Net-Next`).
- On login, `JwtTokenService` issues a signed JWT containing the user id
  (`NameIdentifier`/`sub`), email and role claim.
- Endpoints use `[Authorize]` for authenticated access and
  `[Authorize(Roles = "Admin")]` for admin actions.
- The signing key, issuer and audience come from configuration (`Jwt` section),
  overridable via environment variables.
- User-owned resources derive ownership from the token (e.g. an event's `OrganizerId`
  and an RSVP's `UserId` come from the JWT, never from the request body).
- Disabled accounts (`User.IsActive == false`) are rejected at login.

## Consequences

- Stateless auth that scales and works cleanly with the SPA.
- Clients cannot impersonate other users when creating events or RSVPs.
- Tokens remain valid until expiry; disabling a user prevents new logins but does not
  revoke an already-issued token (acceptable for this project's scope).
