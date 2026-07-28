# 10. Use a layered API test strategy with fast in-memory integration tests

Date: 2026-07-28

## Status

Accepted

## Context

The project needs repeatable verification for core backend behavior such as
authentication, authorization, event retrieval, and request validation. Those checks
should run quickly for local development and coursework review, without requiring a
live SQL Server container, browser automation, or third-party services.

At the same time, tests must cover more than isolated methods: reviewers need
confidence that routing, middleware, controllers, dependency injection, and seeded
data work together correctly.

## Decision

Adopt a layered backend-focused test strategy:

- **Unit tests** cover service-level business logic in isolation using **Moq** for
  repository and collaborator dependencies.
- **Integration tests** exercise HTTP endpoints through
  `WebApplicationFactory<Program>`, so requests pass through the real ASP.NET Core
  pipeline.
- In the test host, replace the production `AppDbContext` registration with EF Core's
  **InMemory** provider, create a fresh database for each run, and seed it using
  `DbInitializer`.
- Keep external dependencies out of automated tests where possible; end-to-end
  validation of the full Docker stack remains documented separately in
  `EVIDENCE.md` and the manual UAT plan.

This means automated tests are optimized for fast feedback on API behavior, while the
full stack is verified through documented acceptance testing rather than browser-based
UI automation.

## Consequences

- Test runs stay fast and self-contained, which encourages frequent execution.
- Integration tests validate routing, filters, serialization, authorization behavior,
  and controller wiring more realistically than pure unit tests.
- Using EF Core InMemory reduces setup friction but does not perfectly match SQL
  Server behavior; query translation, relational constraints, and migration-specific
  issues can still escape automated coverage.
- Because frontend and Docker behavior are not browser-automated, confidence in those
  areas depends on the maintained UAT and evidence artefacts.
