namespace Document.Application.Abstractions;

/// <summary>
/// Persists raw file content. Implementations are swappable storage backends - a local-disk
/// placeholder today, Azure Blob Storage later - so Application never depends on where or how
/// bytes are physically stored.
/// </summary>
public interface IFileStorage
{
    /// <returns>An implementation-specific location (local path, blob URI, ...) that can later be
    /// used to retrieve the content. Callers must treat it as an opaque value.</returns>
    Task<string> SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken);
}
