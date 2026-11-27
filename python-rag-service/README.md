# Python RAG service

This FastAPI service extracts text from uploaded PDFs, creates embeddings through Ollama's OpenAI-compatible API, ranks document chunks, and generates answers with citations.

## Prerequisites

- Python 3.11 or newer.
- Ollama running at `http://localhost:11434`.
- The `nomic-embed-text` and `llama3.2` models installed:

  ```powershell
  ollama pull nomic-embed-text
  ollama pull llama3.2
  ```

## Install and run

From this directory, create and activate a virtual environment, install dependencies, and start the service:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
uvicorn main:app --host 127.0.0.1 --port 8001 --reload
```

Open <http://localhost:8001/docs> to inspect the API. The service provides `POST /ingest` and `POST /answer`; it does not expose a health endpoint.

The service uses the Python `openai` client against Ollama's local OpenAI-compatible endpoint. The `api_key="ollama"` value is a placeholder required by the client library; no cloud API key is used.
