# Enterprise RAG Document Assistant

A local document question-answering application with an Angular frontend, an ASP.NET Core API, a Python/FastAPI RAG service, SQL Server storage, and Ollama for local language models.

## Repository layout

- `frontend/` — Angular web application.
- `RagApi/` — ASP.NET Core API and SQL Server persistence.
- `python-rag-service/` — PDF text extraction, embeddings, retrieval, and answer generation.
- `database/` — SQL Server database initialization script.
- `docs/` — Project guide.

## Prerequisites

- Node.js and npm compatible with the Angular version in `frontend/package.json`.
- Python 3.11 or newer.
- .NET 11 SDK.
- SQL Server.
- Ollama with the `nomic-embed-text` and `llama3.2` models.

## Initial setup

1. Create the database and tables by running [`database/Step1.sql`](database/Step1.sql) in SQL Server.
2. Start Ollama and download the models:

   ```powershell
   ollama pull nomic-embed-text
   ollama pull llama3.2
   ```

3. Configure the API's SQL Server connection string in the environment. In PowerShell, set it in the same terminal where you run the API:

   ```powershell
   $env:ConnectionStrings__SqlServer = "Server=localhost;Database=EnterpriseRagDb;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

   An example settings file is provided at [`RagApi/appsettings.example.json`](RagApi/appsettings.example.json). Do not put real passwords or credentials in tracked settings files. The Python service URL defaults to `http://localhost:8001`; to override it, set `$env:PythonService__BaseUrl`.

## Run locally

Start each component in a separate terminal.

### Python RAG service

```powershell
cd python-rag-service
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
uvicorn main:app --host 127.0.0.1 --port 8001 --reload
```

The interactive API documentation is at <http://localhost:8001/docs>. Ollama must be running locally at `http://localhost:11434`.

### ASP.NET Core API

From the repository root, set the connection string as shown above, then:

```powershell
cd RagApi
dotnet run
```

The API listens on `http://localhost:5000` with the included launch settings.

### Angular frontend

```powershell
cd frontend
npm ci
npm start
```

Open <http://localhost:4200>. The frontend calls the API at `http://localhost:5000`.

## API endpoints

- `GET /api/documents` — list documents.
- `POST /api/documents/upload?domain=ClinicalTrial` — upload a PDF using the `file` form field.
- `POST /api/chat/ask` — ask a question about indexed documents.

See the component READMEs for additional details. The project guide is in [`docs/`](docs/).
