# 9. Use React + Vite (client-side rendering) for the frontend

Date: 2026-07-14

## Status

Accepted

## Context

The brief allows either a React + Vite SPA (SSR off) or ASP.NET Core MVC with Razor
views. The frontend must call the API using JWT tokens and be served in the Docker
stack.

## Decision

Use **React 19 + Vite 8**, client-side rendered (no SSR), with:

- **React Router** for routing and route guards (`ProtectedRoute`),
- **axios** with a global `Authorization: Bearer <token>` header,
- an `AuthContext` that stores the JWT in `localStorage` and decodes it for role/user,
- **Bootstrap 5** for styling.

The built static assets are served by **Nginx**, which also reverse-proxies `/api`,
`/uploads` and `/swagger` to the API so the browser uses a single origin.

## Consequences

- Clear separation between the SPA and the API; the API stays a pure JSON service.
- CSR keeps hosting simple (static files behind Nginx) and avoids a Node runtime in
  production.
- SEO and first-paint are not priorities for this app, so the lack of SSR is acceptable.
- The JWT lives in `localStorage`, which is convenient but susceptible to XSS; acceptable
  for this project's scope and noted for future hardening.
