using System.Text.Json.Serialization;

namespace RagApi.Models;

public record DocumentDto(
    Guid DocumentId,
    string FileName,
    string Domain,
    string Status,
    int PageCount,
    DateTime UploadedAt
);

public record AskRequestDto(
    string Question,
    string? Domain = null,
    Guid? DocumentId = null,
    int TopK = 5
);

public record CitationDto(
    [property: JsonPropertyName("citation_id")] string CitationId,
    [property: JsonPropertyName("chunk_id")] string ChunkId,
    [property: JsonPropertyName("document_id")] string DocumentId,
    [property: JsonPropertyName("file_name")] string FileName,
    [property: JsonPropertyName("page_number")] int PageNumber,
    [property: JsonPropertyName("section_title")] string? SectionTitle,
    [property: JsonPropertyName("excerpt")] string Excerpt
);

public record AskResponseDto(
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("citations")] List<CitationDto> Citations
);

public record PythonChunkPayload(
    [property: JsonPropertyName("chunk_index")] int ChunkIndex,
    [property: JsonPropertyName("page_number")] int PageNumber,
    [property: JsonPropertyName("section_title")] string? SectionTitle,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("embedding")] float[] Embedding
);

public record PythonIngestResponse(
    [property: JsonPropertyName("page_count")] int PageCount,
    [property: JsonPropertyName("chunks")] List<PythonChunkPayload> Chunks
);

public record PythonCandidateChunk(
    [property: JsonPropertyName("chunk_id")] string ChunkId,
    [property: JsonPropertyName("document_id")] string DocumentId,
    [property: JsonPropertyName("file_name")] string FileName,
    [property: JsonPropertyName("page_number")] int PageNumber,
    [property: JsonPropertyName("section_title")] string? SectionTitle,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("embedding")] float[] Embedding
);