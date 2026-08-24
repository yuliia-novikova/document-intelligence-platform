# Roadmap

## Milestone 1 — Current MVP

**Status:** Complete

**Goal**
Establish the Clean Architecture foundation and prove the core ingestion pattern — upload, persist, retrieve — before any cloud dependency is introduced.

**Features**
- Clean Architecture project structure (`Domain` / `Application` / `Contracts` / `Infrastructure` / `Api` / `Processing`) with an enforced, project-reference-based dependency rule
- PostgreSQL persistence via EF Core, with migrations checked into source control
- `POST /documents` (upload) and `GET /documents/{id}` (metadata retrieval)
- `IObjectStorage` abstraction with `LocalFileStorage` as its only implementation
- Health check endpoints (`/health`, `/health/live`, `/health/ready`)
- Local development environment via Podman

**Success criteria**
- A document can be uploaded and its metadata retrieved end-to-end against a local PostgreSQL instance
- `Document.Application` and `Document.Domain` have zero references to `Document.Infrastructure`, ASP.NET Core, or EF Core types
- `/health/ready` reports `Unhealthy` when PostgreSQL is unreachable while `/health/live` does not
- All EF Core migrations apply cleanly to a fresh database

---

## Milestone 2 — Azure Blob Storage

**Status:** Not started

**Goal**
Make Blob Storage a viable production storage backend by adding it as a second `IObjectStorage` implementation, with no change to Application code.

**Features**
- `AzureBlobStorage` implementing `IObjectStorage` via the Azure Blob Storage SDK
- Managed Identity authentication — no connection strings or account keys in configuration
- A `Storage:Provider` configuration switch (`Local` / `AzureBlob`) selected once at startup
- A documented, one-time migration path for moving existing local files to Blob Storage

**Success criteria**
- Switching `Storage:Provider` from `Local` to `AzureBlob` requires no change to `Document.Application`, `Document.Domain`, or endpoint code
- Upload and retrieval behave identically against Blob Storage and local disk, verified against a real Azure Storage account
- No Blob Storage connection string or account key appears anywhere in source control or configuration

---

## Milestone 3 — Background Processing

**Status:** Not started

**Goal**
Decouple ingestion from processing with an asynchronous pipeline that advances a document's status after upload. Running `Document.Processing` as a genuinely separate, independently scaled service depends on shared storage (Milestone 2) being in place; it can be developed and tested against local storage first if both services run on one machine.

**Features**
- `Document.Processing` implemented as a functioning worker (currently an empty scaffold)
- A messaging mechanism (Azure Service Bus) connecting `Document.Api` (publisher) to `Document.Processing` (consumer)
- Status transitions (`Uploaded` → `Processing` → `Processed`/`Failed`) driven by the worker via `IDocumentRepository`
- Retry and failure handling for a message that cannot be processed

**Success criteria**
- A document uploaded via the API transitions to `Processing` and then `Processed` or `Failed` without manual intervention
- `Document.Api` and `Document.Processing` can be deployed and scaled independently
- A processing failure sets `FailureReason` and never leaves a document silently stuck in `Processing`

---

## Milestone 4 — Azure AI Foundry

**Status:** Not started

**Goal**
Perform real, automated extraction/analysis of document content as part of the background processing pipeline.

**Features**
- `Document.Processing` reads content via `IObjectStorage.OpenReadAsync` and submits it to Azure AI Foundry
- Extraction results captured and persisted (schema to be defined)
- Error handling for AI Foundry failures (timeouts, unsupported content, quota limits) distinguished from storage or database failures

**Success criteria**
- A supported document type uploaded through the API has extracted content available for retrieval once processing completes
- AI Foundry failures are recorded with a distinguishable `FailureReason` and do not crash the worker or block subsequent documents
- Extraction latency and failure rate are observable (depends on Milestone 6)

---

## Milestone 5 — Authentication

**Status:** Not started

**Goal**
Move from an unauthenticated prototype to real access control on both the public API and internal service-to-service calls.

**Features**
- Service-to-service authentication via Microsoft Entra ID between `Document.Api` and `Document.Processing`
- Managed Identity for all Azure resource access (Blob Storage, Key Vault, Service Bus, AI Foundry), replacing any remaining secrets
- Azure Key Vault as the source of remaining configuration secrets in deployed environments, replacing user secrets/environment variables
- A defined authorization model for the public API surface

**Success criteria**
- No unauthenticated caller can reach `POST /documents` or `GET /documents/{id}` in a deployed environment
- No connection string, account key, or client secret exists in application configuration in any deployed environment
- Revoking an identity's role assignment immediately removes its access, verified by testing

---

## Milestone 6 — Observability

**Status:** Not started

**Goal**
Make the running system's behavior — latency, errors, throughput — visible without attaching a debugger, across both services.

**Features**
- OpenTelemetry instrumentation for tracing and metrics in `Document.Api` and `Document.Processing`
- Distributed tracing across the Api → Service Bus → Processing → AI Foundry flow, so one document's journey can be followed end-to-end
- Structured logging correlated with trace IDs
- Export to Azure Monitor / Application Insights

**Success criteria**
- A single document upload can be traced end-to-end across both services in one view
- An AI Foundry or storage failure is visible as an alertable signal, not something someone has to find in logs
- Health status and key metrics (upload rate, processing latency, failure rate) are visible on a dashboard

---

## Milestone 7 — Cloud Deployment

**Status:** Not started

**Goal**
Run the system in Azure as its primary deployment target, replacing local Podman-based development as the only way to run it.

**Features**
- `Document.Api` and `Document.Processing` deployed as separate Azure Container Apps within one Container Apps environment
- Azure Database for PostgreSQL (Flexible Server) replacing the local Podman-hosted instance
- A CI/CD pipeline building container images, applying migrations, and deploying on merge to `main`
- Infrastructure as code for the Azure resources involved

**Success criteria**
- A merge to `main` results in a working deployment with no manual steps
- The deployed system passes the same health checks (`/health/live`, `/health/ready`) that Container Apps probes against
- The entire environment can be provisioned from scratch, reproducibly, from source-controlled definitions
