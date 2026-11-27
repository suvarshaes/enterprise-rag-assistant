using RagApi.Models;

namespace RagApi.Repositories;

internal interface IDocumentRepository
{
    Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task CreateProcessingDocumentAsync(Guid documentId, string fileName, string domain, CancellationToken cancellationToken = default);
    Task MarkDocumentReadyAsync(Guid documentId, int pageCount, CancellationToken cancellationToken = default);
    Task MarkDocumentFailedAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task InsertChunksAsync(Guid documentId, IReadOnlyList<PythonChunkPayload> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PythonCandidateChunk>> GetCandidateChunksAsync(string? domain, Guid? documentId, int maxRows = 200, CancellationToken cancellationToken = default);
}
