# Document Intelligence Platform — Architecture

**Legend:** solid blue = implemented today · dashed gray = planned / future

```mermaid
flowchart LR
    classDef current fill:#e8f4ff,stroke:#1a73e8,stroke-width:2px,color:#000
    classDef future fill:#f5f5f5,stroke:#999,stroke-width:2px,stroke-dasharray:5 5,color:#555
    A[Implemented today]:::current
    B[Planned / future]:::future
```

---

## 1. High-Level System Architecture

```mermaid
flowchart TB
    classDef current fill:#e8f4ff,stroke:#1a73e8,stroke-width:2px,color:#000
    classDef future fill:#f5f5f5,stroke:#999,stroke-width:2px,stroke-dasharray:5 5,color:#555

    Client["Client / API Consumer"]:::current

    Api["Document.Api<br/>ASP.NET Core Minimal API"]:::current
    Proc["Document.Processing<br/>Worker host (scaffolded, logic not yet implemented)"]:::future
    DB[("PostgreSQL")]:::current
    Local["LocalFileStorage<br/>(local disk)"]:::current

    Blob[("Azure Blob Storage")]:::future
    KV["Azure Key Vault"]:::future
    AI["Azure AI Foundry"]:::future
    SB["Azure Service Bus"]:::future
    OTel["OpenTelemetry / Azure Monitor"]:::future
    Entra["Microsoft Entra ID<br/>Service-to-Service Auth"]:::future

    Client -->|HTTPS| Api
    Api --> DB
    Api --> Local
    Api -.->|future| Blob
    Api -.->|future| KV
    Api -.->|future| SB
    Api -.->|future| OTel
    Client -.->|future| Entra
    SB -.->|future| Proc
    Proc -.->|future| AI
    Proc -.->|future| Blob
    Proc -.->|future| DB
    Proc -.->|future| OTel
```

Today, `Document.Api` is the only active entry point: it validates and persists uploaded documents to PostgreSQL and local disk. `Document.Processing` exists as an empty worker-host project with no processing logic yet. Everything Azure-labeled is unimplemented — `IObjectStorage` and the Clean Architecture boundaries exist specifically so these can be added without touching Application or Domain code.

---

## 2. Clean Architecture Layer Diagram

```mermaid
flowchart TB
    classDef current fill:#e8f4ff,stroke:#1a73e8,stroke-width:2px,color:#000
    classDef future fill:#f5f5f5,stroke:#999,stroke-width:2px,stroke-dasharray:5 5,color:#555

    subgraph L4["Composition Roots"]
        Api["Document.Api<br/>Minimal API endpoints, DI wiring"]:::current
        Proc["Document.Processing<br/>Worker host"]:::future
    end

    subgraph L3["Infrastructure"]
        Infra["Document.Infrastructure<br/>DocumentDbContext · DocumentRepository<br/>LocalFileStorage"]:::current
        InfraFuture["AzureBlobStorage (future adapter)"]:::future
    end

    subgraph L2["Application"]
        App["Document.Application<br/>IDocumentRepository · IObjectStorage<br/>DocumentService · DocumentUploadValidator"]:::current
    end

    subgraph L1["Domain"]
        Dom["Document.Domain<br/>Document entity · DocumentStatus"]:::current
    end

    subgraph L0["Contracts"]
        Con["Document.Contracts<br/>DocumentResponse"]:::current
    end

    Api --> Infra
    Api --> App
    Api --> Con
    Proc -.-> Infra
    Proc -.-> App
    Proc -.-> Con
    Infra --> App
    Infra --> Dom
    Infra --> Con
    InfraFuture -.implements.-> App
    App --> Dom
    App --> Con
```

Dependencies point inward only: `Domain` has zero dependencies, `Application` depends only on `Domain`/`Contracts`, and `Infrastructure` depends on `Application` to implement its ports (`IDocumentRepository`, `IObjectStorage`) — never the reverse. `AzureBlobStorage` will slot into `Infrastructure` beside `LocalFileStorage` as a second `IObjectStorage` implementation with no change to any inner layer.

---

## 3. Upload Request Sequence Diagram

```mermaid
sequenceDiagram
    participant C as Client
    participant EP as DocumentEndpoints (Api)
    participant VAL as DocumentUploadValidator
    participant SVC as DocumentService (Application)
    participant OBJ as IObjectStorage (port)
    participant LOCAL as LocalFileStorage (Infrastructure)
    participant REPO as IDocumentRepository
    participant DB as PostgreSQL

    C->>EP: POST /documents (multipart file)
    EP->>VAL: Validate(fileName, contentType, length)
    VAL-->>EP: ValidationResult

    alt validation fails
        EP-->>C: 400 Bad Request (ValidationProblem)
    else validation succeeds
        EP->>SVC: CreateAsync(DocumentUploadRequest)
        SVC->>SVC: storageKey = "documents/{guid:N}"
        SVC->>OBJ: SaveAsync(storageKey, stream)
        OBJ->>LOCAL: (current implementation)
        LOCAL-->>OBJ: ok
        OBJ-->>SVC: ok
        SVC->>SVC: new Document(id, ..., storageKey)
        SVC->>REPO: AddAsync + SaveChangesAsync
        REPO->>DB: INSERT
        DB-->>REPO: ok
        REPO-->>SVC: ok
        SVC-->>EP: DocumentResponse
        EP-->>C: 201 Created + Location header
    end
```

Entirely current behavior, verified end-to-end against a live PostgreSQL instance. Note `SaveAsync` returns nothing interpretable — `DocumentService` generates the opaque `StorageKey` itself and never receives a path or URL back.

---

## 4. Future Processing Pipeline

```mermaid
flowchart LR
    classDef current fill:#e8f4ff,stroke:#1a73e8,stroke-width:2px,color:#000
    classDef future fill:#f5f5f5,stroke:#999,stroke-width:2px,stroke-dasharray:5 5,color:#555

    Api["Document.Api<br/>(current: upload endpoint)"]:::current
    SB["Azure Service Bus<br/>DocumentUploaded message"]:::future
    Proc["Document.Processing<br/>consumer (future logic)"]:::future
    Storage["IObjectStorage<br/>(current port)"]:::current
    AI["Azure AI Foundry<br/>extraction / analysis"]:::future
    Repo["IDocumentRepository<br/>(current port)"]:::current
    DB[("PostgreSQL")]:::current

    Api -->|"1 publish"| SB
    SB -->|"2 consume"| Proc
    Proc -->|"3 OpenReadAsync(StorageKey)"| Storage
    Proc -->|"4 submit content"| AI
    AI -->|"5 extraction result"| Proc
    Proc -->|"6 MarkProcessed / MarkFailed"| Repo
    Repo --> DB
```

None of steps 1–6 exist yet. The design intent: `Document.Api` publishes a message after step 6 of the upload flow above; `Document.Processing` consumes it, reads the file through the *same* `IObjectStorage` port already in place (no new storage code needed), sends it to AI Foundry, and updates `Document.Status` via the *same* `IDocumentRepository` already in place. The processing pipeline is designed to reuse existing Application-layer ports rather than invent parallel ones.

---

## 5. Deployment Diagram

```mermaid
flowchart TB
    classDef current fill:#e8f4ff,stroke:#1a73e8,stroke-width:2px,color:#000
    classDef future fill:#f5f5f5,stroke:#999,stroke-width:2px,stroke-dasharray:5 5,color:#555

    subgraph Today["Current — Local Development"]
        Dev["Developer machine"]:::current
        ApiProc["Document.Api process<br/>(dotnet run)"]:::current
        PgContainer["PostgreSQL<br/>Podman container"]:::current
        Disk["Local disk<br/>uploaded-documents/"]:::current

        Dev --> ApiProc
        ApiProc --> PgContainer
        ApiProc --> Disk
    end

    subgraph Prod["Future — Azure Production"]
        ACR["Azure Container Registry"]:::future
        ACA["Azure Container Apps Environment"]:::future
        ApiApp["Document.Api<br/>(Container App)"]:::future
        ProcApp["Document.Processing<br/>(Container App)"]:::future
        PG["Azure Database for PostgreSQL<br/>Flexible Server"]:::future
        Blob["Azure Blob Storage"]:::future
        KV["Azure Key Vault"]:::future
        SB["Azure Service Bus"]:::future
        Entra["Microsoft Entra ID<br/>Managed Identity"]:::future
        Monitor["Azure Monitor /<br/>Application Insights"]:::future

        ACR -.image pull.-> ApiApp
        ACR -.image pull.-> ProcApp
        ACA --- ApiApp
        ACA --- ProcApp
        ApiApp --> PG
        ApiApp --> Blob
        ApiApp --> SB
        ApiApp -.-> KV
        ApiApp -.-> Entra
        ApiApp -.-> Monitor
        ProcApp --> PG
        ProcApp --> Blob
        ProcApp --> SB
        ProcApp -.-> KV
        ProcApp -.-> Entra
        ProcApp -.-> Monitor
    end
```

Today's "deployment" is a developer running `Document.Api` directly on the host against a single Podman-hosted PostgreSQL container — no orchestration, no containerized Api. The future column shows both services running as Azure Container Apps under one environment, each with a Managed Identity (no connection strings or account keys in configuration), pulling from Azure Container Registry, and authenticating to PostgreSQL/Blob Storage/Key Vault/Service Bus via that identity — consistent with the "never hardcode secrets" and Managed Identity requirements already in place for local dev connection strings.
