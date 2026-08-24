namespace Document.Application.Documents;

public interface IDocumentRepository
{
    Task AddAsync(Domain.Entities.Document document, CancellationToken cancellationToken);

    Task<Domain.Entities.Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
