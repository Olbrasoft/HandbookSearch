# CLAUDE.md

## Overview

HandbookSearch - Semantic search for engineering-handbook using embeddings and PostgreSQL pgvector.

## Build & Test

```bash
cd ~/Olbrasoft/HandbookSearch
dotnet build
dotnet test
```

## Architecture

3-tier architecture following Olbrasoft conventions:

| Layer | Project | Purpose |
|-------|---------|---------|
| API | HandbookSearch.AspNetCore.Api | Search endpoint, Swagger |
| Web UI | HandbookSearch.Web | Simple HTML/JS search interface |
| Data | HandbookSearch.Data | Entities, DTOs, interfaces |
| Data (EF) | HandbookSearch.Data.EntityFrameworkCore | DbContext, migrations, pgvector |
| Business | HandbookSearch.Business | Services (Import, Search, Embedding) |
| CLI | HandbookSearch.Cli | Command-line import tool |

## Dependencies

- PostgreSQL with pgvector extension
- Ollama (localhost:11434) with nomic-embed-text model

## Project Structure

```
HandbookSearch/
├── src/
│   ├── HandbookSearch.Data/
│   │   └── Entities/
│   ├── HandbookSearch.Data.EntityFrameworkCore/
│   │   └── Configurations/
│   ├── HandbookSearch.Business/
│   │   └── Services/
│   └── HandbookSearch.Cli/
├── tests/
│   ├── HandbookSearch.Data.Tests/
│   └── HandbookSearch.Business.Tests/
└── HandbookSearch.sln
```

## Namespaces

All namespaces use `Olbrasoft.` prefix:
- `Olbrasoft.HandbookSearch.Data`
- `Olbrasoft.HandbookSearch.Data.EntityFrameworkCore`
- `Olbrasoft.HandbookSearch.Business`
- `Olbrasoft.HandbookSearch.Cli`
