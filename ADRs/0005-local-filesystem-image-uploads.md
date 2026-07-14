# 5. Store event images on the local filesystem

Date: 2026-07-14

## Status

Accepted

## Context

Events can have an image. The brief mandates **local file storage** (no cloud object
storage), and images must survive container restarts.

## Decision

Uploaded images are written to `wwwroot/uploads` inside the API container and served
as static files. The upload endpoint:

- accepts a multipart file from any authenticated user;
- validates extension **and** content-type against an allow-list and enforces a 5 MB
  size limit;
- writes with a server-generated random file name (`{guid}{ext}`) to prevent path
  traversal and overwrites, returning the relative URL (`/uploads/{name}`).

In Docker, `wwwroot/uploads` is backed by a named volume (`uploads_data`) so images
persist across restarts and rebuilds. Nginx proxies `/uploads` to the API.

## Consequences

- No external dependency; fully local as required.
- Persistence is tied to the Docker volume; `docker-compose down -v` intentionally
  clears it.
- Validation reduces the risk of malicious uploads, though this is not a substitute
  for full antivirus scanning in a production system.
