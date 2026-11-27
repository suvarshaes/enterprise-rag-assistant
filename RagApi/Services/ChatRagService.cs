using RagApi.Clients;
using RagApi.Models;
using RagApi.Repositories;

namespace RagApi.Services;

internal sealed class ChatRagService(
    IDocumentRepository documentRepository,
    IPythonRagClient pythonRagClient,
    ILogger<ChatRagService> logger) : IChatRagService
{
    public async Task<AskResponseDto> AskAsync(AskRequestDto request, CancellationToken cancellationToken = default)
    {
        var candidates = await documentRepository.GetCandidateChunksAsync(
            request.Domain,
            request.DocumentId,
            maxRows: 200,
            cancellationToken);

        if (candidates.Count == 0)
        {
            logger.LogInformation(
                "No ready document chunks found for chat request. Domain: {Domain}, DocumentId: {DocumentId}",
                request.Domain,
                request.DocumentId);
        }
        else
        {
            logger.LogInformation(
                "Retrieved {CandidateCount} candidate chunks for chat request. Domain: {Domain}, DocumentId: {DocumentId}",
                candidates.Count,
                request.Domain,
                request.DocumentId);
        }

        return await pythonRagClient.AskAsync(
            request.Question,
            request.Domain,
            candidates,
            request.TopK,
            cancellationToken);
    }
}