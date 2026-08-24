# Document Intelligence Platform

## Overview

Document Intelligence Platform is a backend service for uploading documents and tracking their processing lifecycle. It accepts a file upload, persists its metadata to PostgreSQL, stores the file content behind a storage abstraction, and is designed to hand the file off to a background pipeline for AI-based extraction once that pipeline is built.

The repository demonstrates modern backend engineering practices using .NET and Azure technologies: Clean Architecture with an enforced dependency rule, EF Core migrations against PostgreSQL, a storage abstraction designed for a local-to-cloud swap, and container-orchestration-ready health checks.

## Goals

This project demonstrates:

- A Clean Architecture solution structure with a dependency rule enforced by project references, not just convention
- EF Core code-first persistence against PostgreSQL, including migrations checked into source control
- A storage abstraction (`IObjectStorage`) that lets Application code stay unaware of whether files live on local disk or in Azure Blob Storage
- Health check endpoints that distinguish liveness from readiness, matching how Kubernetes/Azure Container Apps actually probe a service
- Configuration-driven, secret-free local development (Podman-hosted PostgreSQL, no credentials in source control)
- A concrete path from a local-only prototype to an Azure-hosted, event-driven processing pipeline

## Scope

Implemented today:

- `POST /documents` — multipart file upload, with validation on file size, content type, and file name
- `GET /documents/{id}` — document metadata retrieval (never file content)
- PostgreSQL persistence via EF Core, with migrations for the `Document` entity
- Local file storage via `IObjectStorage` / `LocalFileStorage`, with a GUID-based, opaque storage key
- Health check endpoints (`/health`, `/health/live`, `/health/ready`) backed by a PostgreSQL connectivity check
- Local PostgreSQL via Podman Compose for development

## Planned features

- `AzureBlobStorage` as a second `IObjectStorage` implementation
- A document download endpoint
- `Document.Processing` background worker: consumes uploads and updates document status
- Azure AI Foundry integration for document extraction/analysis
- Azure Service Bus as the trigger between upload and processing
- Azure Key Vault-backed configuration
- Service-to-service authentication (Microsoft Entra ID)
- OpenTelemetry instrumentation
- Azure Container Apps deployment

## Non-goals

- Not a general-purpose file storage service — scoped to documents this platform processes
- No multi-tenancy model
- No frontend or UI — API only
- No end-user authentication (only service-to-service auth is planned)
- No virus/malware scanning of uploaded content
- No chunked or resumable uploads; not designed for very large files

## Architecture

The solution follows Clean Architecture: `Document.Domain` has no dependencies, `Document.Application` depends only on `Domain` and `Contracts`, and `Document.Infrastructure` depends on `Application` to implement its ports rather than the reverse. `Document.Api` and `Document.Processing` are composition roots — the only places allowed to wire concrete implementations to abstractions via dependency injection.

Diagrams (system architecture, layer diagram, upload sequence, planned processing pipeline, deployment) are in [`docs/architecture`](docs/architecture/README.md).

## Technology Stack

- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Podman
- Azure Blob Storage (planned)
- Azure Key Vault (planned)
- Azure AI Foundry (planned)
- OpenTelemetry (planned)

## Running locally

1. Install the .NET 10 SDK and Podman.
2. Start PostgreSQL using the Compose file in `podman/` (copy `podman/.env.example` to `podman/.env` first).
3. Configure the `ConnectionStrings:DocumentDb` connection string via `dotnet user-secrets` for `Document.Api`.
4. Apply EF Core migrations with `dotnet ef database update` against `Document.Infrastructure`.
5. Run `Document.Api` and verify it started correctly via `GET /health`.

## Project Structure

| Project | Responsibility |
|---|---|
| `Document.Domain` | Entities and enums (`Document`, `DocumentStatus`). No dependencies. |
| `Document.Application` | Use cases and ports: `DocumentService`, `IDocumentRepository`, `IObjectStorage`, upload validation. Depends only on `Domain` and `Contracts`. |
| `Document.Contracts` | API-facing DTOs (`DocumentResponse`). No dependencies. |
| `Document.Infrastructure` | EF Core `DbContext`, repository and storage implementations, migrations. Depends on `Application`. |
| `Document.Api` | ASP.NET Core Minimal API host: endpoints, health checks, DI composition root. |
| `Document.Processing` | Background worker host. Scaffolded; processing logic not yet implemented. |
| `tests/` | `Document.Application.Tests`, `Document.IntegrationTests` — scaffolded, not yet populated. |

## Design Principles

- **SOLID** — e.g. `DocumentService` depends on `IDocumentRepository`/`IObjectStorage` interfaces (dependency inversion), and each class has a single, narrow reason to change.
- **Clean Architecture** — dependencies point inward only, enforced by the project reference graph, not just code review discipline.
- **Dependency Injection** — every service, repository, and storage implementation is registered and resolved through the built-in ASP.NET Core container; nothing is constructed with `new` across a layer boundary.
- **Abstractions over infrastructure** — `IObjectStorage` and `IDocumentRepository` let Application code stay ignorant of PostgreSQL, the local filesystem, or (later) Azure Blob Storage.
- **Configuration over code** — connection strings, storage roots, and upload limits are read from configuration (`appsettings.json`, user secrets, environment variables), never hardcoded, so environment-specific behavior needs no redeploy.

## Future Improvements

- Expand automated test coverage: unit tests for `Document.Application`, integration tests against a real PostgreSQL instance
- Add a CI pipeline that builds and tests on every pull request
- Add structured exception-handling middleware (unhandled exceptions currently surface as a bare 500)
- Add resiliency (retry policies) for the future `AzureBlobStorage` adapter
- Add API versioning
- Add rate limiting on the upload endpoint
