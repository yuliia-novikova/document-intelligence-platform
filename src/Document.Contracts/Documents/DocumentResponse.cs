namespace Document.Contracts.Documents;

/// <summary>
/// Wire-level document metadata. Deliberately excludes StoragePath (an internal storage detail,
/// not something clients need or should be able to see) and file content is never included here -
/// clients that need the bytes will call a dedicated download endpoint.
/// </summary>
public sealed record DocumentResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    string? FailureReason);
