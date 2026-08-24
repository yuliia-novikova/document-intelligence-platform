using Document.Application.Abstractions;
using Document.Contracts.Documents;

namespace Document.Application.Documents;

public sealed class DocumentService(IDocumentRepository repository, IFileStorage fileStorage) : IDocumentService
{
    public async Task<DocumentResponse> CreateAsync(DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        var documentId = Guid.CreateVersion7();

        // Sanitize with Path.GetFileName to strip any directory component a hostile or malformed
        // file name might carry, so it can never be used to write outside the storage root.
        var storageKey = $"{documentId}/{Path.GetFileName(request.FileName)}";

        // Write the file before the metadata row: if this fails, nothing is persisted and the
        // caller sees an error. If the later SaveChangesAsync fails instead, the worst case is an
        // orphaned file with no DB row - recoverable by a cleanup job - rather than a DB row
        // pointing at a file that was never written, which GET /documents/{id} could not detect.
        var storagePath = await fileStorage.SaveAsync(storageKey, request.Content, cancellationToken);

        var document = new Domain.Entities.Document(
            documentId,
            request.FileName,
            request.ContentType,
            storagePath,
            DateTime.UtcNow);

        await repository.AddAsync(document, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ToResponse(document);
    }

    public async Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken);

        return document is null ? null : ToResponse(document);
    }

    private static DocumentResponse ToResponse(Domain.Entities.Document document) =>
        new(
            document.Id,
            document.OriginalFileName,
            document.ContentType,
            document.Status.ToString(),
            document.CreatedAtUtc,
            document.ProcessedAtUtc,
            document.FailureReason);
}
