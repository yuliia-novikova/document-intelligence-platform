using Document.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Document.Infrastructure.Storage;

/// <summary>
/// Local-disk placeholder for IFileStorage, used until Azure Blob Storage is wired up. Callers
/// only depend on the IFileStorage abstraction, so swapping this out later needs no changes
/// above the Infrastructure layer.
/// </summary>
public sealed class LocalFileStorage(IOptions<LocalFileStorageOptions> options) : IFileStorage
{
    private readonly string _rootPath = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_rootPath, storageKey);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return fullPath;
    }
}
