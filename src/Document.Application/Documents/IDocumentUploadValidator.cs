namespace Document.Application.Documents;

public interface IDocumentUploadValidator
{
    DocumentUploadValidationResult Validate(string? fileName, string? contentType, long length);
}
