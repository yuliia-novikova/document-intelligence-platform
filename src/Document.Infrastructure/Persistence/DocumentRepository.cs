using Document.Application.Documents;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Persistence;

public sealed class DocumentRepository(DocumentDbContext dbContext) : IDocumentRepository
{
    public async Task AddAsync(Domain.Entities.Document document, CancellationToken cancellationToken)
    {
        await dbContext.Documents.AddAsync(document, cancellationToken);
    }

    public Task<Domain.Entities.Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
