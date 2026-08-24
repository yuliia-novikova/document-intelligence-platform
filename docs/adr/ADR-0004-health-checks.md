# ADR-0004: Health Checks

# Status

Accepted

# Context

The service is intended to run under container orchestration (Kubernetes-shaped probes today, Azure Container Apps later). A single "is it up" endpoint conflates two distinct questions an orchestrator asks: "should this instance be restarted?" and "should traffic be routed to this instance?" — and answering both with a database check means a slow or temporarily unavailable PostgreSQL instance triggers unnecessary container restarts.

# Decision

Expose three endpoints: `/health` (runs every registered check, for dashboards/manual inspection), `/health/live` (runs zero checks — healthy whenever the process can respond at all), and `/health/ready` (runs checks tagged `"ready"`, currently a PostgreSQL connectivity check registered via `AddDbContextCheck`). Liveness is deliberately independent of every dependency.

# Alternatives considered

- **A single `/health` endpoint checking the database.** Rejected: a temporarily unavailable database would cause an orchestrator to restart the container, which does not fix a database problem and adds restart churn on top of it.
- **A third-party health check UI/dashboard package.** Rejected: `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` plus a small custom JSON response writer built on `System.Text.Json` satisfied the requirement; no additional package was justified.
- **Including exception details in the response body.** Rejected: these endpoints are conventionally reachable without authentication by infrastructure probes, so surfacing raw exception messages (which can include connection details) would leak information to any caller able to reach the endpoint.

# Consequences

- Verified directly: stopping the PostgreSQL container causes `/health` and `/health/ready` to return 503 while `/health/live` continues returning 200 — the orchestrator-facing distinction was confirmed to actually hold, not just assumed.
- Every future dependency check (Blob Storage, Service Bus) must be explicitly tagged `"ready"` or it silently fails to gate readiness — an easy mistake when a new check is added later without reading this decision first.

# Trade-offs

Three endpoints are more to configure and document (in Kubernetes manifests or Container Apps probe settings) than one. `/health` and `/health/ready` currently run identical checks and only diverge in intent — `/health`'s distinct value today is as a human-facing endpoint, not something an orchestrator should be pointed at, which only pays off once more checks with mixed tags exist.
