# Coding Guidelines

# General Principles

- **Clean Code** — intention-revealing names (`DocumentUploadValidator`, not `Validator1`), small methods with one job. `DocumentService.CreateAsync` orchestrates the upload; it delegates validation, storage, and persistence rather than doing all three inline.
- **SOLID** — in particular, dependency inversion: `DocumentService` depends on `IDocumentRepository` and `IObjectStorage`, never on EF Core or filesystem types directly.
- **KISS** — build the simplest thing that satisfies today's requirement. A two-endpoint API doesn't need a mediator/CQRS pipeline.
- **YAGNI** — `IObjectStorage` exposes `SaveAsync`, `OpenReadAsync`, `DeleteAsync` — not `ExistsAsync` or provider-specific members — because nothing calls them yet. Add a method when a real caller needs it.
- **DRY** — applies to knowledge, not incidental similarity. Three similar-looking lines don't need a shared helper; an abstraction used by exactly one caller isn't DRY, it's indirection.

# Architecture Rules

- **Dependencies must point inward.** `Domain` has zero project references. `Infrastructure` depends on `Application`, never the reverse — this is enforced by the project reference graph, not code review discipline.
- **Infrastructure must never leak into Domain.** Entities carry no EF Core attributes, no provider-specific types. All persistence mapping lives in `IEntityTypeConfiguration<T>` classes in `Infrastructure`.
- **Configuration through the Options pattern.** Bind to a typed options class (`DocumentUploadOptions`, `LocalFileStorageOptions`) via `IOptions<T>`; don't read `IConfiguration` keys scattered through business logic.
- **Dependency Injection only.** Every service, repository, and adapter is registered in the container. Nothing is constructed with `new` across a layer boundary. Composition roots (`Api`, `Processing`) are the only projects allowed to know which concrete type backs an interface.

# Error Handling

- Expected failures — validation, "not found" — return a typed result (`DocumentUploadValidationResult`) or a nullable return value, mapped to the correct HTTP status at the API boundary. Don't use exceptions for expected control flow.
- Unexpected failures (a database or storage outage) propagate. Don't catch an exception just to log it and rethrow, and don't swallow it into a silently-degraded state.
- Never leak exception details — stack traces, connection strings, internal paths — to a client. Health check and error responses return a status and a message, not `Exception.ToString()`.
- Fail fast on missing required configuration, with an actionable message, at startup — not a `NullReferenceException` deep inside a request handler.

# Logging

- Inject `ILogger<T>`. Never `Console.WriteLine`, never a static/global logger instance.
- Use structured logging — message templates with named parameters (`"Document {DocumentId} uploaded"`), not string interpolation into the message — so log data stays queryable.
- Log at the point a decision is made (a validation rejection, a caught expected exception), not redundantly at every layer a value passes through.
- Never log secrets, connection strings, or full server-side file paths.

# Validation

- Validate at the boundary, before any side effect. `DocumentUploadValidator` runs before `IObjectStorage.SaveAsync` is ever called — nothing is written until the input is known-good.
- Return field-specific, actionable error messages, not a generic "invalid request."
- Use a dedicated validator type rather than inline checks scattered across a handler, so the rule is unit-testable in isolation.
- Mirror persistence constraints (e.g., max length) at the validation boundary, so a violation surfaces as `400`, not an opaque database error as `500`.

# Async Programming

- Every I/O-bound method is `async` and accepts a `CancellationToken`, threaded through to the underlying call.
- Never call `.Result` or `.Wait()` on a `Task` — it blocks a thread and risks deadlocks.
- Don't wrap a genuinely synchronous, fast operation in `Task.Run` to fake asynchrony (e.g., a local file delete has no async OS API — call it directly and return `Task.CompletedTask`).
- Use `await using` for `IAsyncDisposable` resources (streams, and similar).

# Dependency Injection

- Register the narrowest correct lifetime. Default to `Scoped` for anything used per-request or that depends — even transitively — on a `Scoped` `DbContext`.
- Never register a `Singleton` that captures a `Scoped` dependency — a captive-dependency bug that only shows up under concurrent load.
- Constructor injection only. No service locator, no pulling `IServiceProvider` into business logic.

# Entity Framework

- One `IEntityTypeConfiguration<T>` per entity. Don't let `OnModelCreating` grow unbounded with inline configuration.
- Map explicitly wherever the provider's default is wrong for the intent — e.g., PostgreSQL/Npgsql's default `DateTime` mapping is `timestamp without time zone`; a UTC value needs an explicit `timestamptz` column type or it throws at runtime.
- Generate entity IDs in application code (`ValueGeneratedNever`) when identity must exist before the first save, not via database identity.
- Every schema change is a checked-in migration. No manual DDL against a shared database.
- Don't return `IQueryable<T>` from a repository. Materialize results so query construction stays behind one boundary.

# API Design

- Return the status code that matches what happened: `201` with a `Location` header on creation, `404` for a missing resource, `400` with structured, field-level errors for invalid input.
- A response DTO is not the persistence model. Map explicitly; never serialize an entity directly.
- Don't leak internal details in a response. This API's `DocumentResponse` omits the storage key entirely, not just the file content.
- Keep request/response contracts in a project with no dependencies, so any layer can reference them without pulling in unrelated code.

# Testing Strategy

- Unit test `Application`-layer logic (services, validators) against interface fakes — no database, no web host.
- Integration test anything touching EF Core/PostgreSQL against a real database, not an in-memory provider — provider-specific behavior (e.g., PostgreSQL column type mapping) won't be caught by one.
- Test observable behavior through public members, not private implementation details.
- A bug fix ships with a regression test, not after the fact.

# Git Commit Convention

Use [Conventional Commits](https://www.conventionalcommits.org/): `<type>(<optional scope>): <description>`.

Common types: `feat`, `fix`, `refactor`, `docs`, `chore`, `test`.

- Explain *why* in the body — the diff already shows *what* changed.
- One logical change per commit. Don't bundle an unrelated formatting pass into a feature commit.

# Pull Request Checklist

- [ ] Builds with zero warnings
- [ ] New or changed behavior is covered by a test, or a reason is given why not
- [ ] No secrets, connection strings, or credentials in the diff
- [ ] Migrations included for any entity/schema change, and verified against a real database
- [ ] Public API changes are reflected in relevant documentation
- [ ] Commit messages follow Conventional Commits

# Code Review Checklist

- [ ] Dependencies still point inward — no new reference from an inner layer to an outer one
- [ ] No business logic added to a composition root that belongs in `Application`
- [ ] New abstractions are justified by an actual second caller or a stated near-term need, not speculative reuse
- [ ] Async and `CancellationToken` used consistently for new I/O-bound code
- [ ] No hardcoded values that should be configuration
- [ ] Error handling matches this project's convention — typed result for expected failures, propagate for unexpected ones
