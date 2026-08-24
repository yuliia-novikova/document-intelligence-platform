# Document Intelligence Platform

You are helping build a production-quality .NET backend project.

## Tech Stack

- .NET 10
- ASP.NET Core
- PostgreSQL
- EF Core
- Podman
- Clean Architecture

## Architecture

Follow Clean Architecture.

Dependencies must point inward.

Application must never depend on Infrastructure.

Infrastructure implements Application abstractions.

Configuration through Options pattern.

Dependency Injection only.

## Coding Style

Use async APIs.

Use CancellationToken.

Prefer immutable models where appropriate.

Use constructor injection.

Avoid static state.

Do not generate unnecessary abstractions.

Keep implementations production-ready.

## Storage

Development uses LocalFileStorage.

Production will use Azure Blob Storage.

Never expose storage implementation details outside Infrastructure.

Store StorageKey instead of StoragePath.

## Future Roadmap

Azure Blob Storage

Azure Key Vault

Azure AI Foundry

Background Processing

Service-to-Service Authentication

OpenTelemetry

Azure Container Apps

## When generating code

Always explain architectural decisions.

Do not modify unrelated files.

Prefer small incremental changes.

Suggest a Conventional Commit message.
