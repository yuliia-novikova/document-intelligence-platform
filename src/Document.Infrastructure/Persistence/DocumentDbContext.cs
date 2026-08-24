using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Persistence;

public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.Document> Documents => Set<Domain.Entities.Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
