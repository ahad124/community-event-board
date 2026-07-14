# 7. Docker Compose topology (SQL Server + API + Nginx frontend)

Date: 2026-07-14

## Status

Accepted

## Context

A reviewer must be able to run the whole stack with a single `docker-compose up`,
reachable at `http://localhost`. The frontend is a static SPA that calls the API.

## Decision

Three services in `docker-compose.yml`:

1. **sqlserver** — `mcr.microsoft.com/mssql/server:2022-latest`, with a healthcheck
   and the `mssql_data` volume.
2. **api** — multi-stage .NET 8 build; depends on `sqlserver` being healthy; runs on
   port 8080; mounts the `uploads_data` volume for images.
3. **frontend** — Vite build served by **Nginx** on port 80 (mapped to
   `http://localhost`). Nginx serves the SPA and reverse-proxies `/api`, `/uploads`
   and `/swagger` to the API, giving the browser a single origin.

HTTPS redirection is made opt-in (off by default) so the stack works over plain HTTP
inside containers, and CORS allowed origins are configurable via environment.

## Consequences

- One command brings up the full stack; `docker-compose down -v` gives a clean slate.
- Single-origin routing avoids CORS complexity in the browser.
- The setup targets local review, not production hardening (self-signed TLS, secrets
  management, etc. are out of scope).
