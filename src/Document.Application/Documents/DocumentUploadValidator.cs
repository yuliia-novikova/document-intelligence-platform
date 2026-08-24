using Microsoft.Extensions.Options;

namespace Document.Application.Documents;

public sealed class DocumentUploadValidator(IOptions<DocumentUploadOptions> options) : IDocumentUploadValidator
{
    private const int MaxFileNameLength = 260;

    private readonly DocumentUploadOptions _options = options.Value;

    public DocumentUploadValidationResult Validate(string? fileName, string? contentType, long length)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return DocumentUploadValidationResult.Failure("A file name is required.");
        }

        if (fileName.Length > MaxFileNameLength)
        {
            return DocumentUploadValidationResult.Failure(
                $"The file name exceeds the maximum allowed length of {MaxFileNameLength} characters.");
        }

        if (length <= 0)
        {
            return DocumentUploadValidationResult.Failure("The uploaded file is empty.");
        }

        if (length > _options.MaxFileSizeBytes)
        {
            return DocumentUploadValidationResult.Failure(
                $"The uploaded file exceeds the maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        if (string.IsNullOrWhiteSpace(contentType) ||
            !_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return DocumentUploadValidationResult.Failure(
                $"Content type '{contentType}' is not supported.");
        }

        return DocumentUploadValidationResult.Success();
    }
}
