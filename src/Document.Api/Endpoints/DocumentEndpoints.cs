using Document.Application.Documents;
using Document.Contracts.Documents;

namespace Document.Api.Endpoints;

public static class DocumentEndpoints
{
    private const string GetDocumentByIdRouteName = "GetDocumentById";

    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/documents").WithTags("Documents");

        group.MapPost("/", CreateDocumentAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<DocumentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            // No antiforgery services are registered app-wide, so this has no effect today; it
            // documents that this endpoint is an API for programmatic/API clients (not a browser
            // form post) and keeps it correctly exempt if antiforgery is ever added globally.
            .DisableAntiforgery();

        group.MapGet("/{id:guid}", GetDocumentByIdAsync)
            .WithName(GetDocumentByIdRouteName)
            .Produces<DocumentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateDocumentAsync(
        IFormFile file,
        IDocumentUploadValidator validator,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var validationResult = validator.Validate(file.FileName, file.ContentType, file.Length);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = [validationResult.ErrorMessage!]
            });
        }

        await using var stream = file.OpenReadStream();
        var request = new DocumentUploadRequest(stream, file.FileName, file.ContentType, file.Length);

        var response = await documentService.CreateAsync(request, cancellationToken);

        return Results.CreatedAtRoute(GetDocumentByIdRouteName, new { id = response.Id }, response);
    }

    private static async Task<IResult> GetDocumentByIdAsync(
        Guid id,
        IDocumentService documentService,
        CancellationToken cancellationToken)
    {
        var response = await documentService.GetByIdAsync(id, cancellationToken);

        return response is null ? Results.NotFound() : Results.Ok(response);
    }
}
