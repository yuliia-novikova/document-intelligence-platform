using Document.Contracts.Documents;

namespace Document.Application.Documents;

public interface IDocumentService
{
    Task<DocumentResponse> CreateAsync(DocumentUploadRequest request, CancellationToken cancellationToken);

    Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
