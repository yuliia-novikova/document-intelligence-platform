using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Document.Infrastructure.Persistence;

/// <summary>
/// Used only by the `dotnet ef` CLI to build <see cref="DocumentDbContext"/> at design time,
/// so migrations can be authored before Document.Api wires up real DI/connection strings.
/// </summary>
public sealed class DocumentDbContextFactory : IDesignTimeDbContextFactory<DocumentDbContext>
{
    public DocumentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DOCUMENTDB_CONNECTION")
            ?? throw new InvalidOperationException(
                "The DOCUMENTDB_CONNECTION environment variable must be set to run design-time EF Core commands, " +
                "e.g. Host=localhost;Port=5432;Database=documentintelligence;Username=postgres;Password=<your-local-password>");

        var optionsBuilder = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseNpgsql(connectionString);

        return new DocumentDbContext(optionsBuilder.Options);
    }
}
