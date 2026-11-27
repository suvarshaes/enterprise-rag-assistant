using RagApi.Clients;
using RagApi.Models;
using RagApi.Repositories;

namespace RagApi.Services;

internal sealed class DocumentIngestionService(
    IDocumentRepository documentRepository,
    IPythonRagClient pythonRagClient,
    ILogger<DocumentIngestionService> logger) : IDocumentIngestionService
{
    public Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default)
        => documentRepository.GetAllAsync(cancellationToken);

    public async Task<UploadResult> UploadAndIndexAsync(
        Stream fileStream,
        string fileName,
        string domain,
        CancellationToken cancellationToken = default)
    {
        var docId = Guid.NewGuid();
        await documentRepository.CreateProcessingDocumentAsync(docId, fileName, domain, cancellationToken);
        logger.LogInformation(
            "Started document ingestion for {DocumentId} in domain {Domain}",
            docId,
            domain);

        try
        {
            var ingestResult = await pythonRagClient.IngestPdfAsync(fileStream, fileName, domain, cancellationToken);
            await documentRepository.InsertChunksAsync(docId, ingestResult.Chunks, cancellationToken);
            await documentRepository.MarkDocumentReadyAsync(docId, ingestResult.PageCount, cancellationToken);
            logger.LogInformation(
                "Document ingestion completed for {DocumentId}: {PageCount} pages and {ChunkCount} chunks",
                docId,
                ingestResult.PageCount,
                ingestResult.Chunks.Count);

            return new UploadResult(docId, "Ready", ingestResult.PageCount, ingestResult.Chunks.Count);
        }
        catch (Exception ingestionException)
        {
            logger.LogError(ingestionException, "Document ingestion failed for {DocumentId}", docId);
            try
            {
                await documentRepository.MarkDocumentFailedAsync(docId, CancellationToken.None);
            }
            catch (Exception statusException)
            {
                logger.LogError(statusException, "Could not mark failed document {DocumentId} as Failed", docId);
                throw new AggregateException(
                    $"Document {docId} ingestion failed and its status could not be updated.",
                    ingestionException,
                    statusException);
            }

            throw;
        }
    }
}