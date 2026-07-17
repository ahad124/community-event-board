# Code Review — PR #1 "Add unit and integration tests with coverage validation"

- **Repository:** `msami25/EventBoard`
- **PR:** [#1](https://github.com/msami25/EventBoard/pull/1) · `feature/testing-coverage` → `main`
- **Author:** @msami25 · **Reviewer:** ahad124 · **Date:** 2026-07-16
- **Size:** 83 files, +12,056 / −1

## Verdict: 🔴 Request changes

The **test code itself is solid** — clean AAA structure, good use of Moq, sensible
unit + integration split, and the integration tests correctly exercise the
`201/204/401/403` authorization paths against the real pipeline. I verified the
expectations against the app: `EventsController` returns `CreatedAtAction` (201)
and `NoContent` (204), `public partial class Program {}` is present, and `Program`
skips startup seeding under the `Testing` environment — all consistent with the
tests.

However, I'm requesting changes for two reasons: (1) a **PR-hygiene/scope problem**
that makes the change very hard to review and risks committing generated/binary
artefacts, and (2) a **fragile integration-test seeding pattern** that can silently
break. Neither is hard to fix.

---

## ✅ Strengths

- **Clear AAA + naming.** `Method_Scenario_ExpectedResult` throughout
  (`CreateEventAsync_WithNullEvent_ThrowsArgumentNullException`) — very readable.
- **Good authorization coverage in integration tests.** `PostEvent` is tested for
  Admin (201), unauthenticated (401), and non-admin (403). That 401-vs-403
  distinction is exactly what teams get wrong; nice.
- **`TestAuthHandler` is a clean way to fake roles** without real JWTs, and the
  factory swaps SQL Server for InMemory correctly.
- **Meaningful assertions**, not just "not null": the refresh-token test asserts the
  cookie is `HttpOnly` and that the token is *absent from the body* — that's a real
  security property worth locking down.
- Coverage tooling (coverlet) and an AI-prompts log are included.

---

## 🔴 Blocking

### B1. PR scope & hygiene — the deliverable is buried in generated/duplicated content
The PR is titled "tests + coverage" but the diff is **83 files / +12k lines**, of
which the actual deliverable is ~10 test files. The rest includes:
- an entire **`backend/`** directory (appears to be a second copy of the API),
- **18 `backend/docs/*.md`** files,
- **binary uploads** `backend/wwwroot/uploads/*.png`,
- generated **HTML coverage report** output.

**Why it matters:** reviewers can't reason about a 12k-line diff; binaries and
generated reports bloat history and cause noisy future diffs; a duplicated
`backend/` tree is a merge hazard.

**Ask:**
- Scope this PR to the test project (`EventBoard.Tests/`) + the minimal config it
  needs. Split unrelated app/docs changes into their own PR(s).
- `.gitignore` the generated coverage output, `wwwroot/uploads/`, `bin/`, `obj/`,
  and `TestResults/`. Commit a coverage *summary*, not the HTML tree.
- Confirm whether `backend/` is an intentional restructure or an accidental
  duplicate of `EventBoard.Api/`; if accidental, remove it.

### B2. Integration-test DB seeding via `BuildServiceProvider()` is fragile
`CustomWebApplicationFactory.ConfigureServices` calls
`services.BuildServiceProvider()` and seeds through that scope. This is the ASP.NET
Core **ASP0000** anti-pattern: it builds a *second* container, and with EF Core
InMemory the seeded rows can land in a **different store** than the one the app
resolves at request time (InMemory stores are keyed per internal service provider
unless a shared `InMemoryDatabaseRoot` is supplied). Because `Program` skips
seeding under `Testing`, this factory seeding is the *only* source of data — so if
the stores ever diverge, `GetEvents_...Assert.True(events.Length >= 3)` fails with
no obvious cause.

**Ask — seed after the host is built, using the app's own provider:**
```csharp
// in the test (or an override), not inside ConfigureServices:
var factory = new CustomWebApplicationFactory();
using (var scope = factory.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Seed(db);
}
```
or pass a shared `InMemoryDatabaseRoot` to `UseInMemoryDatabase(dbName, root)`.
This is the documented pattern and removes the ambiguity.

---

## 🟠 Major (should fix)

### M1. Shared mutable state across integration tests
`EventsApiTests` uses `IClassFixture<CustomWebApplicationFactory>`, so **one DB is
shared by every test in the class**, and several tests mutate it (`PostEvent` adds,
`PutEvent` renames Event 1). Tests currently pass only because the assertions are
loose (`Length >= 3`, `Id == 1`), but this is order-dependent and will bite later.
Prefer a fresh database per test (reset in a fixture `Dispose`/constructor, or
`IAsyncLifetime`), or assert against IDs the test itself created.

### M2. Coverage gaps on the highest-risk code
Coverage is concentrated on `EventService`/`JwtTokenService`, but several
security-sensitive areas in this codebase have **no tests**:
- **Booking ownership / IDOR** (can user A read user B's booking?).
- **Refresh-token rotation & the token blacklist** (`RefreshTokenRepository`,
  `ITokenBlacklist`) — core to the auth model.
- **File-upload validation** (`FileValidator`, `FileStorageService`) — type/size
  limits are classic vulnerabilities.
Add at least happy-path + one negative test for each. "100% of `EventService`" is
less valuable than one test proving a non-owner gets `403`.

### M3. InMemory provider hides relational behaviour
EF Core InMemory doesn't enforce relational constraints or translate raw SQL, so
these tests won't catch issues in anything using `FromSqlRaw`, indexes, or FK/cascade
rules (e.g. the reports query). For integration tests that should mirror production,
consider **SQLite in-memory** or **Testcontainers for SQL Server**. At minimum, note
this limitation so it isn't mistaken for full-stack coverage.

---

## 🟡 Minor / nits

- **`TestAuthHandler`**: `ISystemClock` is obsolete in .NET 8 (`CS0618`); switch to
  the `TimeProvider`-based `AuthenticationHandler<T>(IOptionsMonitor, ILoggerFactory,
  UrlEncoder)` constructor. Also `Authorization.ToString().Replace("Test ", "")`
  replaces *every* occurrence in the string; parse the scheme instead
  (`AuthenticationHeaderValue.TryParse`) for robustness.
  *(`EventBoard.Tests/TestHelpers/TestAuthHandler.cs`)*
- **`TestDataFactory.GetValidEventDto` / `GetInvalidEventDto`** return `object`, are
  unused, and reference validation that isn't asserted anywhere — dead code; remove
  or turn into real negative-validation tests.
  *(`EventBoard.Tests/TestHelpers/TestDataFactory.cs`)*
- **Integration tests deserialize to the EF entity** (`ReadFromJsonAsync<Event[]>`)
  rather than a response DTO — this couples the tests to the entity shape and to
  serializing navigation properties. Prefer a small response contract/DTO.
  *(`EventBoard.Tests/IntegrationTests/EventsApiTests.cs`)*
- **`EventServiceTests` comments** like `// Assuming IEventRepository lives here`
  suggest AI-scaffolded imports that were never confirmed — tidy these up now that
  the namespaces are known. *(`EventBoard.Tests/UnitTests/EventServiceTests.cs`)*
- **No cancellation-token / no test for `GetAllEventsAsync` empty case** — cheap
  additions that round out the service tests.

---

## 📋 Inline comments (file : anchor)

| File | Location | Comment |
|------|----------|---------|
| `EventBoard.Tests/CustomWebApplicationFactory.cs` | `services.BuildServiceProvider()` (seed block) | **B2** — build a second provider; seed via `factory.Services.CreateScope()` after creation, or pass a shared `InMemoryDatabaseRoot`. |
| `EventBoard.Tests/IntegrationTests/EventsApiTests.cs` | class fixture / `PutEvent...`, `PostEvent...` | **M1** — shared mutable DB across tests; isolate per test or assert on self-created IDs. |
| `EventBoard.Tests/IntegrationTests/EventsApiTests.cs` | `ReadFromJsonAsync<Event[]>()` | Nit — deserialize to a DTO, not the EF entity. |
| `EventBoard.Tests/TestHelpers/TestAuthHandler.cs` | ctor `ISystemClock`; `Replace("Test ", "")` | Nit — obsolete `ISystemClock`; fragile header parse. |
| `EventBoard.Tests/TestHelpers/TestDataFactory.cs` | `GetValidEventDto` / `GetInvalidEventDto` | Nit — unused `object`-returning dead code. |
| `EventBoard.Tests/UnitTests/EventServiceTests.cs` | `using ... // Assuming ...` | Nit — remove speculative "assuming" comments. |
| PR-wide | `backend/`, `backend/docs/*`, `wwwroot/uploads/*.png`, coverage HTML | **B1** — out of scope / generated / binary; gitignore and split. |

---

## Suggested checklist before merge
- [ ] Reduce the diff to the test project (+ minimal config); split the rest.
- [ ] `.gitignore` coverage output, `bin/obj`, `TestResults/`, `wwwroot/uploads/`.
- [ ] Fix the factory seeding (post-build scope or shared `InMemoryDatabaseRoot`).
- [ ] Isolate integration tests from shared mutable state.
- [ ] Add negative/authorization tests for bookings (IDOR), refresh tokens, uploads.
- [ ] Remove dead helpers and obsolete `ISystemClock`.
- [ ] Confirm CI runs `dotnet test` and publishes the coverage summary.

Nice work overall — the testing fundamentals are here; this is mostly about
**scoping the PR** and **hardening the integration harness** so the suite stays
trustworthy as the codebase grows.
