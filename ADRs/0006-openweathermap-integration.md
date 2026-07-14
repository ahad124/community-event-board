# 6. Use OpenWeatherMap for event weather

Date: 2026-07-14

## Status

Accepted

## Context

The event detail page must show weather for the event's city using a free
third-party API. External calls can fail or be slow, and the API key must not be
hard-coded.

## Decision

Integrate **OpenWeatherMap**:

- Current conditions via `/data/2.5/weather` and a short outlook via the 5-day/3-hour
  `/data/2.5/forecast` endpoint (one snapshot per day, up to 4 days).
- Use a typed `HttpClient` configured in `Program.cs` with a **Polly** retry policy for
  transient failures and a 10s timeout.
- The API key is read from configuration (`OpenWeather:ApiKey`), supplied via
  environment variable in Docker.
- The city is derived best-effort from the free-text event location (segment before
  the first comma).
- All failures are swallowed and surfaced as `Available = false`, so the UI shows a
  graceful "weather unavailable" state instead of erroring.

## Consequences

- Weather is resilient: retries handle blips, and a missing key or upstream outage
  never breaks the page.
- City extraction is heuristic and may be imperfect for unusual location strings.
