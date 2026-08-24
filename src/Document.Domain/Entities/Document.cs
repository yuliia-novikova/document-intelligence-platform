using Document.Domain.Enums;

namespace Document.Domain.Entities;

public sealed class Document
{
    public Guid Id { get; private set; }

    public string OriginalFileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    /// <summary>
    /// Opaque identifier understood only by the configured IObjectStorage implementation (a local
    /// path today, a blob name later). Domain and Application must never interpret this value.
    /// </summary>
    public string StorageKey { get; private set; } = null!;

    public DocumentStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    private Document()
    {
        // Required by EF Core for materialization.
    }

    public Document(Guid id, string originalFileName, string contentType, string storageKey, DateTime createdAtUtc)
    {
        Id = id;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        StorageKey = storageKey;
        Status = DocumentStatus.Uploaded;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkProcessing()
    {
        Status = DocumentStatus.Processing;
    }

    public void MarkProcessed(DateTime processedAtUtc)
    {
        Status = DocumentStatus.Processed;
        ProcessedAtUtc = processedAtUtc;
        FailureReason = null;
    }

    public void MarkFailed(string failureReason, DateTime processedAtUtc)
    {
        Status = DocumentStatus.Failed;
        FailureReason = failureReason;
        ProcessedAtUtc = processedAtUtc;
    }
}
