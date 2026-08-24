# ADR-0003: PostgreSQL

# Status

Accepted

# Context

The platform needs durable, relational storage for document metadata (identity, file name, content type, status, timestamps) — a small, structured, single-row-per-document shape. The database choice also had to work well for both local development and the project's stated Azure production target.

# Decision

Use PostgreSQL, accessed through EF Core with the Npgsql provider. Local development runs PostgreSQL in a Podman container; schema changes are checked-in EF Core migrations applied via `dotnet ef`.

# Alternatives considered

- **SQL Server.** Rejected: no compelling advantage here, and PostgreSQL has an equally first-class managed offering on the project's Azure target (Azure Database for PostgreSQL Flexible Server) without licensing considerations.
- **A document database (e.g. Cosmos DB).** Rejected: document metadata is relational and structured, not document-shaped; EF Core's relational tooling (migrations, strongly-typed configuration) is more mature than its Cosmos provider for this use case.
- **Dapper instead of EF Core.** Rejected: migrations-as-code and `IEntityTypeConfiguration<T>` keeping mapping concerns out of the entity outweighed Dapper's lower overhead at this project's current scale.

# Consequences

- Schema changes are explicit, reviewable migrations rather than manual DDL — verified in practice when `Document.StoragePath` was renamed to `Document.StorageKey` and EF Core generated a data-preserving `RenameColumn` migration automatically.
- Local development has a hard dependency on a running PostgreSQL instance (via Podman) — there is no in-memory or SQLite fallback, so anything touching persistence needs the real database.
- `Microsoft.EntityFrameworkCore.Relational` had to be explicitly pinned in `Document.Infrastructure`, because the Npgsql provider's own minimum version wasn't otherwise raised by transitive resolution, leaving downstream projects with a mismatched Core/Relational pair that only showed up by inspecting the resolved dependency graph directly.

# Trade-offs

PostgreSQL-specific behavior already surfaced once in practice: Npgsql's default `DateTime` mapping is `timestamp without time zone`, which throws at runtime for a value with `Kind=Utc` unless the column is explicitly mapped to `timestamptz`. This is a real cost specific to this database choice that a SQL Server-based design would not have surfaced the same way, traded off against PostgreSQL's fit with the Azure target.
