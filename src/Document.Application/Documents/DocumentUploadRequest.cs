namespace Document.Application.Documents;

/// <summary>
/// Carries raw upload data across the Api -> Application boundary. Deliberately built from
/// primitive/BCL types (Stream, string, long) rather than Microsoft.AspNetCore.Http.IFormFile,
/// so Application stays testable without a web host and has no dependency on the web framework.
/// </summary>
public sealed record DocumentUploadRequest(Stream Content, string FileName, string ContentType, long Length);
