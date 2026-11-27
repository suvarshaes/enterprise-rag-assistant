using RagApi.Models;

namespace RagApi.Services;

internal sealed record UploadResult(Guid DocumentId, string Status, int PageCount, int ChunksIndexed);

internal interface IDocumentIngestionService
{
    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default);
    Task<UploadResult> UploadAndIndexAsync(Stream fileStream, string fileName, string domain, CancellationToken cancellationToken = default);
}