# ADR-0001: Clean Architecture

# Status

Accepted

# Context

The platform's roadmap adds several external dependencies over time — Azure Blob Storage, Azure Key Vault, Azure AI Foundry — on top of the PostgreSQL persistence already in place. Without an enforced boundary, code written against these dependencies tends to spread into business logic, making that logic untestable without the dependency present and making each new integration a wider-reaching change than it needs to be.

This was not a hypothetical concern: during development, `Document.Application` was found to directly reference `Document.Infrastructure` (backwards, given Infrastructure is meant to depend on Application to implement its interfaces), which would have produced a circular reference the moment Infrastructure needed to reference Application back.

# Decision

Split the solution into six projects with dependencies enforced by project references, not convention: `Document.Domain` (no dependencies), `Document.Application` (depends on Domain and Contracts only), `Document.Contracts` (no dependencies), `Document.Infrastructure` (depends on Application), and `Document.Api` / `Document.Processing` (depend on Application, Infrastructure, and Contracts, as composition roots). Application defines ports (`IDocumentRepository`, `IObjectStorage`); Infrastructure implements them.

# Alternatives considered

- **Single project.** Faster to start, but nothing prevents ASP.NET Core or EF Core types from leaking into business logic, and unit-testing that logic would require a running web host and database.
- **Layered folders within one project.** Folder conventions (`/Domain`, `/Application`, `/Infrastructure`) document intent but the compiler doesn't enforce them — a backward reference compiles without complaint.
- **CQRS / vertical-slice with a mediator library.** Considered given the project's stated preference for vertical slices where appropriate, but rejected for now: the API surface is two endpoints, and adding a mediator dependency isn't justified by the current complexity.

# Consequences

- New Azure integrations are added as new Infrastructure classes implementing existing Application ports, with no change to Application or Domain.
- Application and Domain are unit-testable without a web host or a real database.
- Six projects and explicit project references add navigation and setup overhead disproportionate to a two-endpoint API today.

# Trade-offs

The backwards-reference incident shows the ceremony isn't purely theoretical protection — a real violation occurred with only one feature implemented. The cost is paid continuously (more projects to open, more reference edits when adding a project); the benefit is paid out later, each time a new outer-layer dependency is added without touching inner layers.
