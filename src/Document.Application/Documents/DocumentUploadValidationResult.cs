namespace Document.Application.Documents;

public sealed record DocumentUploadValidationResult(bool IsValid, string? ErrorMessage)
{
    public static DocumentUploadValidationResult Success() => new(true, null);

    public static DocumentUploadValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
