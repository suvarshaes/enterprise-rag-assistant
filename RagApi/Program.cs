using RagApi;
using RagApi.Clients;
using RagApi.Models;
using RagApi.Repositories;
using RagApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:SqlServer");

var pythonBaseUrl = builder.Configuration["PythonService:BaseUrl"] ?? "http://localhost:8001";

builder.Services.AddHttpClient("PythonRag", client =>
{
    client.BaseAddress = new Uri(pythonBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(3);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddScoped<IDocumentRepository>(_ => new SqlDocumentRepository(connectionString));
builder.Services.AddScoped<IPythonRagClient, PythonRagClient>();
builder.Services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
builder.Services.AddScoped<IChatRagService, ChatRagService>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors();

app.MapGet("/api/documents", async (IDocumentIngestionService ingestionService, CancellationToken ct) =>
{
    var docs = await ingestionService.GetDocumentsAsync(ct);
    return Results.Ok(docs);
});

app.MapPost("/api/documents/upload", async (
    IFormFile file,
    string domain,
    IDocumentIngestionService ingestionService,
    CancellationToken ct) =>
{
    if (file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["file"] = ["Choose a non-empty PDF file."]
        });
    }

    if (string.IsNullOrWhiteSpace(domain) || domain.Length > 50)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["domain"] = ["Domain is required and must be 50 characters or fewer."]
        });
    }

    await using var stream = file.OpenReadStream();
    var result = await ingestionService.UploadAndIndexAsync(stream, Path.GetFileName(file.FileName), domain, ct);
    return Results.Ok(new
    {
        documentId = result.DocumentId,
        status = result.Status,
        pageCount = result.PageCount,
        chunksIndexed = result.ChunksIndexed
    });
}).DisableAntiforgery();

app.MapPost("/api/chat/ask", async (
    AskRequestDto request,
    IChatRagService chatRagService,
    CancellationToken ct) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        errors["question"] = ["Enter a question."];
    }

    if (request.TopK is < 1 or > 20)
    {
        errors["topK"] = ["TopK must be between 1 and 20."];
    }

    if (request.Domain?.Length > 50)
    {
        errors["domain"] = ["Domain must be 50 characters or fewer."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var result = await chatRagService.AskAsync(request, ct);
    return Results.Ok(result);
});

app.Run();