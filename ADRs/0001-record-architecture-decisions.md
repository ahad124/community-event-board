# 1. Record architecture decisions

Date: 2026-07-14

## Status

Accepted

## Context

We need a lightweight, durable record of the significant technical decisions made
while building the Community Event Board, so reviewers and future contributors can
understand *why* the system is shaped the way it is, not just *what* it does.

## Decision

We will use Architecture Decision Records (ADRs), one Markdown file per decision,
stored in `docs/adr/` and numbered sequentially. Each ADR captures the context, the
decision, and its consequences. Superseded decisions are kept for history and marked
accordingly.

## Consequences

- Decisions are visible in version control alongside the code they affect.
- The overhead is small; each ADR is a short file.
- Changing a past decision means adding a new ADR that supersedes the old one.
