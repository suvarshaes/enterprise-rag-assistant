using RagApi.Models;

namespace RagApi.Clients;

internal interface IPythonRagClient
{
    Task<PythonIngestResponse> IngestPdfAsync(
        Stream pdfStream,
        string fileName,
        string domain,
        CancellationToken cancellationToken = default);

    Task<AskResponseDto> AskAsync(
        string question,
        string? domain,
        IReadOnlyList<PythonCandidateChunk> candidates,
        int topK,
        CancellationToken cancellationToken = default);
}