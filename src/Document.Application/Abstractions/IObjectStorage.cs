namespace Document.Application.Abstractions;

/// <summary>
/// Persists and retrieves arbitrary byte content addressed by an opaque storage key. Callers
/// generate the key themselves (see Document.StorageKey) and must never attempt to interpret,
/// construct a path/URL from, or otherwise assign meaning to it - only the configured
/// implementation (local disk today, Azure Blob Storage later) knows what a key resolves to.
/// </summary>
public interface IObjectStorage
{
    /// <summary>Writes content under the given key, creating any needed containers/directories.</summary>
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken);

    /// <summary>Opens a readable stream for the content previously saved under the given key.</summary>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>Deletes the content previously saved under the given key.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
