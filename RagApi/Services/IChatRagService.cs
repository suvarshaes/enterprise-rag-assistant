using RagApi.Models;

namespace RagApi.Services;

internal interface IChatRagService
{
    Task<AskResponseDto> AskAsync(AskRequestDto request, CancellationToken cancellationToken = default);
}