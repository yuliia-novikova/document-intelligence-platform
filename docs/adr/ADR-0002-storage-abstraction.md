# ADR-0002: Storage Abstraction

# Status

Accepted

# Context

Uploaded file content must be stored somewhere. Local disk is sufficient for zero-cost development, but production requires Azure Blob Storage, and `Document.Processing` will eventually need to read the same content to submit it to Azure AI Foundry. The goal was for the eventual Blob Storage migration to be an additive change, not a rewrite of Application code.

# Decision

Define `IObjectStorage` (`SaveAsync`, `OpenReadAsync`, `DeleteAsync`) in `Document.Application/Abstractions`, taking only primitive/BCL parameters — a `string` key and a `Stream`. `Document.StorageKey` is an opaque, GUID-based identifier (`documents/{guid:N}`) that `DocumentService` generates and never interprets. `LocalFileStorage` in `Document.Infrastructure/Storage` is the only implementation today; `AzureBlobStorage` will be a second implementation of the same interface.

# Alternatives considered

- **Returning the physical location from `SaveAsync`.** The original design had `SaveAsync` return the resolved path, stored as `Document.StoragePath`. Rejected in a later revision: it handed Application a value shaped entirely by the storage provider, which is exactly what the abstraction was meant to prevent.
- **Storage key containing the original file name.** Rejected: couples the key format to user-supplied input (a path-traversal concern) when the original file name is already preserved separately as `Document.OriginalFileName`.
- **Passing `IFormFile` into Application.** Rejected: `IFormFile` is an ASP.NET Core type; accepting it in `Document.Application` would violate the dependency rule from ADR-0001. `DocumentUploadRequest` (`Stream`, `string`, `string`, `long`) is the translation boundary instead.

# Consequences

- Adding Azure Blob Storage requires one new Infrastructure class and one DI registration change — no change to Application, Domain, or endpoint code.
- `Document.Processing` can reuse the same `IObjectStorage` port to read content for AI Foundry submission, with no processing-specific storage code.
- The abstraction hides real differences between local disk and Blob Storage (network latency, transient failures); code proven against local disk is not thereby proven against Blob Storage.

# Trade-offs

The interface is deliberately minimal — three methods, no `ExistsAsync`, no Azure-specific members (SAS tokens, tiering) — accepting the risk of a future interface change if a genuine need for those appears, in exchange for not designing for needs that don't exist yet. Local storage does not support horizontal scaling — each instance owns its own disk — which is acceptable for a single-instance development setup but is a hard blocker on running more than one instance until Blob Storage replaces it.
