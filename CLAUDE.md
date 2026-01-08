# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

HandbookSearch - Semantic search for engineering-handbook using embeddings and PostgreSQL pgvector.

## Build & Test

```bash
dotnet build
dotnet test

# Run specific test project
dotnet test tests/HandbookSearch.Business.Tests

# Run single test method
dotnet test --filter "FullyQualifiedName~GenerateEmbeddingAsync_ValidText_ReturnsEmbedding"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~EmbeddingServiceTests"
```

## Deployment

```bash
./deploy/deploy.sh
```

Deploys CLI to `/opt/olbrasoft/handbook-search/cli/`. The script builds, tests, and publishes.

## CLI Commands

```bash
# Development (from project directory)
cd src/HandbookSearch.Cli
dotnet run -- import-all --path ~/GitHub/Olbrasoft/engineering-handbook
dotnet run -- import-files --files "workflow/git-workflow.md,testing/unit-testing.md" --translate-cs
dotnet run -- delete-files --files "path.md"

# Production (deployed)
/opt/olbrasoft/handbook-search/cli/HandbookSearch.Cli import-files --files "path.md" --translate-cs
```

## Run API

```bash
cd src/HandbookSearch.AspNetCore.Api
dotnet run
# Swagger UI: http://localhost:5000/swagger
```

## Architecture

```
API (HandbookSearch.AspNetCore.Api) - Minimal API, port 5000
    |
Business (HandbookSearch.Business) - Services: Embedding, Search, DocumentImport, AzureTranslation
    |
Data (HandbookSearch.Data + Data.EntityFrameworkCore) - Document entity, DbContext, pgvector
    |
PostgreSQL with pgvector extension
```

## Dependencies

- PostgreSQL 16+ with pgvector extension
- Ollama (localhost:11434) with `qwen3-embedding:0.6b` model (1024 dimensions)
- Azure Translator (optional, for `--translate-cs` flag)

## Secrets Management

Uses SecureStore for encrypted secrets:

```bash
# Vault location
~/.config/handbook-search/secrets/secrets.json
~/.config/handbook-search/keys/secrets.key

# Add secrets
SecureStore set -s ~/.config/handbook-search/secrets/secrets.json \
  -k ~/.config/handbook-search/keys/secrets.key \
  "Database:Password=your_password"
```

| Secret | Description |
|--------|-------------|
| `Database:Password` | PostgreSQL password (production only) |
| `AzureTranslator:ApiKey` | Azure Translator API key |
| `AzureTranslator:FallbackApiKey` | Azure Translator fallback API key |

## Code Style

- **Namespace prefix:** `Olbrasoft.HandbookSearch.*`
- **File-scoped namespaces:** `namespace Olbrasoft.HandbookSearch.Business.Services;`
- **Testing:** xUnit + Moq (NOT NUnit/NSubstitute)
- **Database columns:** snake_case naming
- **Embeddings:** 1024 dimensions, use `CosineDistance()` for similarity
- **Async methods:** suffix with `Async`, accept `CancellationToken`

## Database & Migrations

```bash
cd src/HandbookSearch.Data.EntityFrameworkCore
dotnet ef migrations add MigrationName
dotnet ef database update
```

## CI/CD

GitHub Actions workflow (`.github/workflows/build.yml`):
- **Build job:** Runs on `ubuntu-latest`, builds and tests (excludes IntegrationTests)
- **Deploy job:** Runs on self-hosted `handbook-search` runner, executes `deploy/deploy.sh`
