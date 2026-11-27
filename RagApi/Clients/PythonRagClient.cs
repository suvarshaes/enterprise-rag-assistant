using System.Net.Http.Headers;
using System.Net.Http.Json;
using RagApi.Models;

namespace RagApi.Clients;

internal sealed class PythonRagClient(
    IHttpClientFactory httpClientFactory,
    ILogger<PythonRagClient> logger) : IPythonRagClient
{
    public async Task<PythonIngestResponse> IngestPdfAsync(
        Stream pdfStream,
        string fileName,
        string domain,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("PythonRag");
        using var form = new MultipartFormDataContent();

        using var fileContent = new StreamContent(pdfStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(domain), "domain");

        using var response = await client.PostAsync("/ingest", form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Python /ingest returned HTTP {StatusCode}", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadFromJsonAsync<PythonIngestResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Python /ingest returned an empty or invalid response.");
    }

    public async Task<AskResponseDto> AskAsync(
        string question,
        string? domain,
        IReadOnlyList<PythonCandidateChunk> candidates,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("PythonRag");
        var payload = new
        {
            question,
            domain,
            candidates,
            top_k = topK
        };

        using var response = await client.PostAsJsonAsync("/answer", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Python /answer returned HTTP {StatusCode}", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadFromJsonAsync<AskResponseDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Python /answer returned an empty or invalid response.");
    }
}