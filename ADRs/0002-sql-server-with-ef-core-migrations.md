# 2. Use SQL Server with EF Core migrations

Date: 2026-07-14

## Status

Accepted

## Context

The project brief requires a local **SQL Server** database running in Docker, with
the schema created via **EF Core migrations**. An earlier prototype used SQLite with
`EnsureCreated()`, which is convenient locally but does not meet the requirement and
provides no versioned schema history.

## Decision

Use `Microsoft.EntityFrameworkCore.SqlServer` against a SQL Server 2022 container.
The schema is defined by EF Core migrations under `EventBoard.Api/Migrations` and
applied at startup with `Database.Migrate()`. The connection string is supplied via
configuration/environment (`ConnectionStrings__DefaultConnection`) so it differs
between local dev and Docker without code changes.

Because the integration tests use the EF Core **InMemory** provider (which does not
support migrations), `DbInitializer` branches on `Database.IsRelational()`: relational
providers run `Migrate()`, others fall back to `EnsureCreated()`.

## Consequences

- Schema changes are explicit, reviewable and repeatable across environments.
- Startup applies pending migrations automatically; a retry loop tolerates SQL Server
  container warm-up time.
- Tests stay fast and dependency-free by using InMemory.
- Raw SQL in the reporting endpoint is now T-SQL and uses `Microsoft.Data.SqlClient`.
