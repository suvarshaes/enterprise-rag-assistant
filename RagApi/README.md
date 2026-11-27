# ASP.NET Core API

The API is organized into the requested layers:

- `Models/` contains API DTOs and Python service contracts.
- `Repositories/` defines the document persistence interface and its SQL Server implementation.
- `Clients/` calls the Python FastAPI `/ingest` and `/answer` endpoints.
- `Services/` coordinates document ingestion and question answering.
- `Program.cs` configures dependency injection and maps the HTTP routes to application services.

## Configuration and run

Create the database and tables using [`database/Step1.sql`](../database/Step1.sql), then start SQL Server and the Python service. Set the SQL Server connection string outside tracked files. For PowerShell, set it in the terminal used to run the API:

```powershell
$env:ConnectionStrings__SqlServer = "Server=localhost;Database=EnterpriseRagDb;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet run
```

For other configuration options, see [`appsettings.example.json`](appsettings.example.json) and the [repository setup guide](../README.md).

## HTTP endpoints

- `GET /api/documents` — lists stored documents.
- `POST /api/documents/upload` — uploads a PDF using the `file` form field and optional `domain` query parameter. The document is persisted as `Processing`, ingested by Python, its chunks are saved, then its status becomes `Ready` (or `Failed` if ingestion/persistence fails).
- `POST /api/chat/ask` — accepts JSON such as `{ "question": "What are the trial endpoints?", "domain": "ClinicalTrial", "topK": 5 }`. The API retrieves ready chunks, sends them to Python for ranking and answer generation, and returns citations.

Python requests use the snake_case contracts defined by `python-rag-service/main.py`. API response fields use camelCase except citation fields, which retain the Python service's snake_case contract.