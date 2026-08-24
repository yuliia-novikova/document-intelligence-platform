namespace Document.Application.Documents;

public sealed class DocumentUploadOptions
{
    public const string SectionName = "DocumentUpload";

    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;

    public IReadOnlyCollection<string> AllowedContentTypes { get; set; } =
    [
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/tiff",
        "text/plain",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];
}
