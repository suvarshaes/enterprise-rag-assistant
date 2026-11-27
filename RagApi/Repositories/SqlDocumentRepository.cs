using System.Text.Json;
using Microsoft.Data.SqlClient;
using RagApi.Models;

namespace RagApi.Repositories;

internal sealed class SqlDocumentRepository(string connectionString) : IDocumentRepository
{
    public async Task<IReadOnlyList<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var docs = new List<DocumentDto>();

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            SELECT DocumentId, FileName, Domain, Status, PageCount, UploadedAt
            FROM Documents
            ORDER BY UploadedAt DESC
            """;

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            docs.Add(new DocumentDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetDateTime(5)
            ));
        }

        return docs;
    }

    public async Task CreateProcessingDocumentAsync(
        Guid documentId,
        string fileName,
        string domain,
        CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO Documents (DocumentId, FileName, Domain, Status, PageCount)
            VALUES (@DocumentId, @FileName, @Domain, 'Processing', 0)
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@DocumentId", documentId);
        cmd.Parameters.AddWithValue("@FileName", fileName);
        cmd.Parameters.AddWithValue("@Domain", domain);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkDocumentReadyAsync(
        Guid documentId,
        int pageCount,
        CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE Documents
            SET Status = 'Ready', PageCount = @PageCount
            WHERE DocumentId = @DocumentId
            """;

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@DocumentId", documentId);
        cmd.Parameters.AddWithValue("@PageCount", pageCount);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkDocumentFailedAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = "UPDATE Documents SET Status = 'Failed' WHERE DocumentId = @DocumentId";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@DocumentId", documentId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertChunksAsync(
        Guid documentId,
        IReadOnlyList<PythonChunkPayload> chunks,
        CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO DocumentChunks
                (ChunkId, DocumentId, ChunkIndex, PageNumber, SectionTitle, Content, EmbeddingJson)
            VALUES
                (@ChunkId, @DocumentId, @ChunkIndex, @PageNumber, @SectionTitle, @Content, @EmbeddingJson)
            """;

        foreach (var chunk in chunks)
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ChunkId", Guid.NewGuid());
            cmd.Parameters.AddWithValue("@DocumentId", documentId);
            cmd.Parameters.AddWithValue("@ChunkIndex", chunk.ChunkIndex);
            cmd.Parameters.AddWithValue("@PageNumber", chunk.PageNumber);
            cmd.Parameters.AddWithValue("@SectionTitle", (object?)chunk.SectionTitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Content", chunk.Content);
            cmd.Parameters.AddWithValue("@EmbeddingJson", JsonSerializer.Serialize(chunk.Embedding));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<PythonCandidateChunk>> GetCandidateChunksAsync(
        string? domain,
        Guid? documentId,
        int maxRows = 200,
        CancellationToken cancellationToken = default)
    {
        var candidates = new List<PythonCandidateChunk>();

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = $"""
            SELECT TOP ({maxRows})
                c.ChunkId, c.DocumentId, d.FileName, c.PageNumber,
                c.SectionTitle, c.Content, c.EmbeddingJson
            FROM DocumentChunks c
            INNER JOIN Documents d ON c.DocumentId = d.DocumentId
            WHERE d.Status = 'Ready'
            """;

        if (documentId.HasValue)
        {
            sql += " AND c.DocumentId = @DocumentId";
        }
        else if (!string.IsNullOrWhiteSpace(domain))
        {
            sql += " AND d.Domain = @Domain";
        }

        await using var cmd = new SqlCommand(sql, conn);
        if (documentId.HasValue)
        {
            cmd.Parameters.AddWithValue("@DocumentId", documentId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(domain))
        {
            cmd.Parameters.AddWithValue("@Domain", domain);
        }

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var emb = JsonSerializer.Deserialize<float[]>(reader.GetString(6)) ?? [];
            candidates.Add(new PythonCandidateChunk(
                reader.GetGuid(0).ToString(),
                reader.GetGuid(1).ToString(),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetString(5),
                emb
            ));
        }

        return candidates;
    }
}