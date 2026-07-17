# 10. Structured logging with Serilog and Seq

Date: 2026-07-16

## Status

Accepted

## Context

The API was using the default `Microsoft.Extensions.Logging` provider writing to
the console. That is fine for reading a single container's stdout, but it gives us
no way to **search, filter, or correlate** logs: to answer "which requests
returned 500 in the last hour?" or "which endpoint is slow?" you had to scroll
raw text in `docker compose logs`.

The application already logs *structured* events throughout (message templates
with named properties, e.g. `_logger.LogInformation("Login attempt for {Email}", email)`
in [`AuthService.cs`](../EventBoard.Api/Services/AuthService.cs), plus event/RSVP/
favorite events in the controllers). Those named properties were being flattened
to plain strings and lost. We wanted a lightweight, local-friendly way to capture
them as queryable data — and we needed it before running the incident-response
exercises, where fast detection and diagnosis depend on being able to query logs.

## Decision

Adopt **Serilog** as the logging pipeline and ship logs to a **Seq** server.

- **Packages:** `Serilog.AspNetCore`, `Serilog.Sinks.Seq`, `Serilog.Sinks.Console`
  (see [`EventBoard.Api.csproj`](../EventBoard.Api/EventBoard.Api.csproj)).
- **Wiring** ([`Program.cs`](../EventBoard.Api/Program.cs)): `builder.Host.UseSerilog(...)`
  reads configuration, enriches with `FromLogContext`, and writes to **Console +
  Seq**. The Seq URL comes from `Seq:ServerUrl` and is environment-overridable
  (`Seq__ServerUrl`), defaulting to `http://localhost:5341`.
- **Request logging:** `app.UseSerilogRequestLogging()` emits one structured event
  per HTTP request with `RequestMethod`, `RequestPath`, `StatusCode` and `Elapsed`
  (ms). Existing `ILogger<T>` calls flow through unchanged and keep their named
  properties.
- **Configuration** ([`appsettings.json`](../EventBoard.Api/appsettings.json)): a
  `Seq` section for the local default and a `Serilog` section that caps
  `Microsoft.AspNetCore` and `Microsoft.EntityFrameworkCore` at `Warning` to keep
  the stream readable.
- **Topology:** Seq runs as a service in `docker-compose.yml` (`datalust/seq`,
  host `5341:80`, `seq_data` volume). The API ships to `http://seq:80` over the
  compose network; operators open the dashboard at `http://localhost:5341`. Local
  authentication is disabled (`SEQ_FIRSTRUN_NOAUTHENTICATION=true`) for dev
  convenience. This extends the topology in [ADR 0007](0007-docker-compose-topology.md).

Alternatives considered:

- **Console/stdout only** (status quo) — no query/search; rejected.
- **Application Insights / other hosted APM** — richer, but requires a cloud
  account and keys; too heavy for a locally reviewable project.
- **ELK / Grafana Loki** — capable but operationally heavier (multiple containers,
  more memory) than a single Seq container for this scope.

## Consequences

- Every request and custom event is captured as **structured, queryable** data.
  Operators can run filters such as `StatusCode >= 500`, `Elapsed > 1000`, or
  `@Level = 'Error'` to detect and diagnose issues in seconds.
- This directly enabled the incident-response work: the misconfigured-connection
  incident was found via the `SqlException` in Seq, and the slow-endpoint incident
  via `Elapsed > 1000` (see `postmortem-incident-a.md` / `postmortem-incident-b.md`
  and `runbook.md`).
- Trade-offs: one extra container (~modest memory); EF Core SQL is capped at
  `Warning` by default, so diagnosing an N+1 requires temporarily raising the EF
  log level; and Seq auth is disabled for local dev only — a real deployment must
  enable authentication and persist the admin credential.
- Because the app's log call sites were already structured, no application logging
  code had to change to gain the dashboard.
