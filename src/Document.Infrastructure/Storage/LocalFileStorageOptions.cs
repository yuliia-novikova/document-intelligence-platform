namespace Document.Infrastructure.Storage;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "LocalFileStorage";

    public string RootPath { get; set; } = "uploaded-documents";
}
