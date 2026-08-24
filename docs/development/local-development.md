# Local Development

# Prerequisites

- **.NET SDK** — version matching this repository's target framework (`net10.0`).
- **Podman** — runs PostgreSQL locally. This project uses Podman rather than Docker by convention; commands throughout this guide are Podman-flavored but are Docker-CLI-compatible if you substitute `docker` for `podman`.
- **Git**
- **An IDE — either Visual Studio 2026 or VS Code.** Pick one; both are supported. VS Code needs the C# extension (or C# Dev Kit) for debugging and IntelliSense.
- **Claude Code** (optional). This repository includes a `CLAUDE.md` at the root with project-specific conventions Claude Code reads automatically. Not required to build or run the project.

# Local setup

High-level steps — each is detailed further below.

1. Clone the repository.
2. Start PostgreSQL via Podman.
3. Configure the database connection string for `Document.Api` via user secrets.
4. Apply EF Core migrations.
5. Run `Document.Api`.
6. Confirm it started correctly via the health check endpoints.

# Database

PostgreSQL runs in a Podman container, configured by `podman/compose.yml` and `podman/.env` (copy `podman/.env.example` to `podman/.env` first — the real `.env` is gitignored and never committed).

If your Podman installation has a Compose provider available:

```bash
podman compose -f podman/compose.yml up -d
```

If `podman compose` reports no compose provider found (common on a fresh Podman install — neither `podman-compose` nor `docker-compose` ships by default), start the container directly instead, using the same values as `podman/.env`:

```bash
podman run -d --name document-platform-postgres \
  -e POSTGRES_DB=document_platform \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:17
```

Verify it's running and check which host port it's actually bound to — this matters for the connection string in the next step:

```bash
podman ps
```

# Running migrations

EF Core migrations live in `Document.Infrastructure`. Applying them requires a database connection string, supplied only through the `DOCUMENTDB_CONNECTION` environment variable — there is no hardcoded fallback, by design.

```bash
# bash/zsh
export DOCUMENTDB_CONNECTION="Host=localhost;Port=5432;Database=document_platform;Username=postgres;Password=postgres"
```

```powershell
# PowerShell
$env:DOCUMENTDB_CONNECTION = "Host=localhost;Port=5432;Database=document_platform;Username=postgres;Password=postgres"
```

Then, from the repository root:

```bash
dotnet ef database update \
  --project src/Document.Infrastructure/Document.Infrastructure.csproj \
  --startup-project src/Document.Infrastructure/Document.Infrastructure.csproj \
  --context Document.Infrastructure.Persistence.DocumentDbContext
```

Adjust `Port=` if your container is bound to a different host port than 5432 (see `podman ps` above).

# Running the API

`Document.Api` reads its connection string from configuration at runtime — a separate mechanism from the `DOCUMENTDB_CONNECTION` environment variable used above, which only applies to design-time `dotnet ef` commands. Set it once via user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DocumentDb" \
  "Host=localhost;Port=5432;Database=document_platform;Username=postgres;Password=postgres" \
  --project src/Document.Api/Document.Api.csproj
```

Then run the API:

```bash
dotnet run --project src/Document.Api/Document.Api.csproj
```

# Health Checks

Once running, confirm the API started correctly and can reach PostgreSQL:

- `GET /health` — overall status, every registered check (useful for a quick manual look).
- `GET /health/live` — always healthy if the process can respond at all; no dependency checks.
- `GET /health/ready` — includes the PostgreSQL connectivity check; reflects whether the API can actually serve requests.

```bash
curl http://localhost:5289/health
```

A healthy response looks like:

```json
{"status":"Healthy","checks":[{"name":"postgresql","status":"Healthy", "...": "..."}]}
```

# Troubleshooting

**`podman compose` fails with "compose provider not found."**
Neither `podman-compose` nor `docker-compose` is installed. Either install one, or start the container directly with the `podman run` command shown above.

**`dotnet ef` fails with a message about `DOCUMENTDB_CONNECTION` being missing.**
The design-time factory requires this environment variable to be set in the current shell session before running any `dotnet ef` command. It does not read user secrets or `appsettings.json`.

**The API fails to start with "Missing ConnectionStrings:DocumentDb configuration."**
User secrets are scoped per project. Confirm you ran `dotnet user-secrets set` with `--project src/Document.Api/Document.Api.csproj` specifically, not another project.

**`/health/ready` (or `/health`) reports the `postgresql` check as `Unhealthy`.**
The container isn't running, or the connection string's port/credentials don't match it. Run `podman ps` to confirm the container is up and check its actual host port mapping.

**File upload returns `400` with a validation error.**
Check `DocumentUpload:MaxFileSizeBytes` and `DocumentUpload:AllowedContentTypes` in `src/Document.Api/appsettings.json` — uploads outside those limits are rejected before anything is written to storage.

**Port mismatch between `podman/compose.yml` and `podman/.env.example`.**
`compose.yml` currently hardcodes host port `5432`; `.env.example` documents `POSTGRES_PORT=5433` but nothing in `compose.yml` reads that value. Always confirm the actual bound port with `podman ps` rather than assuming either file.
