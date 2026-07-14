# 4. Controller → Service → Repository layering

Date: 2026-07-14

## Status

Accepted

## Context

We want the backend to be testable and to keep data-access concerns out of the HTTP
layer, without over-engineering a small application.

## Decision

Organize the API into thin controllers, application services, and repositories:

- **Controllers** handle HTTP concerns (routing, model binding, status codes, auth).
- **Repositories** encapsulate EF Core queries behind interfaces
  (`IUserRepository`, `IEventRepository`, `IBookingRepository`, `IFavoriteRepository`).
- **Services** (`AuthService`, `JwtTokenService`, `WeatherService`) hold business logic
  and integrations; they are registered via DI and mockable in tests.

Some simple controllers use repositories directly rather than introducing a pass-through
service, to avoid needless indirection.

## Consequences

- Business logic is unit-testable with mocked repositories (see `EventBoard.Api.Tests`).
- EF Core stays confined to the repository layer.
- The pragmatic mix (service where there's logic, repository-direct where there isn't)
  keeps the codebase small and readable.
