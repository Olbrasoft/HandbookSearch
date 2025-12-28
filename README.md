# HandbookSearch

Semantic search for engineering-handbook using embeddings and PostgreSQL pgvector.

## Overview

HandbookSearch provides semantic search capabilities over the Olbrasoft engineering-handbook. It uses embeddings (via Ollama with nomic-embed-text) and PostgreSQL pgvector to find relevant documentation based on natural language queries.

**Purpose:** When AI agents receive a prompt, HandbookSearch automatically provides a list of the most relevant handbook documents, eliminating the need to scan the entire handbook.

## Architecture

3-tier architecture following Olbrasoft conventions:

```
┌─────────────────────────────────────┐
│   ASP.NET Core API (Port 5000)      │  Search endpoint
│   HandbookSearch.AspNetCore.Api     │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Business Layer                    │  Services (Import, Search, Embedding)
│   HandbookSearch.Business           │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Data Layer                        │  Entities, DTOs
│   HandbookSearch.Data               │
│   HandbookSearch.Data.EFCore        │  DbContext, pgvector
└─────────────────────────────────────┘
```

### Projects

| Layer | Project | Purpose |
|-------|---------|---------|
| API | `HandbookSearch.AspNetCore.Api` | Search endpoint, Swagger |
| Business | `HandbookSearch.Business` | Services (Import, Search, Embedding) |
| Data | `HandbookSearch.Data` | Entities, DTOs, interfaces |
| Data (EF) | `HandbookSearch.Data.EntityFrameworkCore` | DbContext, migrations, pgvector |
| CLI | `HandbookSearch.Cli` | Command-line import tool |

## Dependencies

### Required Services

- **PostgreSQL 16+** with pgvector extension
- **Ollama** (localhost:11434) with `nomic-embed-text` model

### Installation

```bash
# PostgreSQL with pgvector
sudo apt install postgresql-16 postgresql-16-pgvector

# Ollama
curl -fsSL https://ollama.com/install.sh | sh

# Pull embedding model
ollama pull nomic-embed-text

# Verify
curl http://localhost:11434/api/tags
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL 16+ with pgvector extension
- Ollama with nomic-embed-text model

### Clone and Build

```bash
git clone https://github.com/Olbrasoft/HandbookSearch.git
cd HandbookSearch
dotnet restore
dotnet build
```

### Database Setup

```bash
# Create database
createdb handbook_search

# Enable pgvector extension
psql -d handbook_search -c "CREATE EXTENSION IF NOT EXISTS vector;"

# Run migrations (when implemented)
cd src/HandbookSearch.Data.EntityFrameworkCore
dotnet ef database update
```

### Configuration

**Development** - Use User Secrets:

```bash
cd src/HandbookSearch.Cli
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=handbook_search;Username=postgres;Password=YourPassword"
dotnet user-secrets set "Ollama:BaseUrl" "http://localhost:11434"
dotnet user-secrets set "Ollama:Model" "nomic-embed-text"
```

**Production** - Use environment variables (see [Secrets Management](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/secrets-management.md))

### Import Handbook Documents

```bash
cd src/HandbookSearch.Cli
dotnet run -- import-all --path ~/GitHub/Olbrasoft/engineering-handbook
```

### Run API

```bash
cd src/HandbookSearch.AspNetCore.Api
dotnet run

# Open Swagger UI
open http://localhost:5000/swagger
```

## Usage

### Search API

```bash
# Search for documents
curl "http://localhost:5000/api/search?q=jak používat git branches&limit=5"
```

**Response:**
```json
{
  "results": [
    {
      "documentId": 1,
      "filePath": "development-guidelines/workflow/git-workflow-workflow.md",
      "title": "Git Workflow",
      "contentSnippet": "Git workflow, GitHub issues, branches...",
      "distance": 0.12
    }
  ]
}
```

### CLI Commands

```bash
# Import all markdown files
dotnet run -- import-all --path ~/GitHub/Olbrasoft/engineering-handbook

# Import specific files
dotnet run -- import-files --files "workflow/git-workflow.md,testing/unit-testing.md"
```

## Running Tests

```bash
dotnet test
```

## Deployment

See [Local Apps Deployment Guide](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/dotnet/continuous-deployment/local-apps-deploy-continuous-deployment.md)

**Self-hosted runner required** - needs access to:
- Local PostgreSQL database
- Local Ollama instance
- engineering-handbook repository

## Project Structure

```
HandbookSearch/
├── .github/
│   └── workflows/
│       ├── build.yml          # Build and test
│       └── deploy.yml         # Deploy to production
├── src/
│   ├── HandbookSearch.Data/
│   │   └── Entities/
│   │       └── Document.cs
│   ├── HandbookSearch.Data.EntityFrameworkCore/
│   │   ├── HandbookSearchDbContext.cs
│   │   └── Migrations/
│   ├── HandbookSearch.Business/
│   │   └── Services/
│   │       ├── EmbeddingService.cs
│   │       ├── SearchService.cs
│   │       └── DocumentImportService.cs
│   ├── HandbookSearch.AspNetCore.Api/
│   │   ├── Controllers/
│   │   └── Program.cs
│   └── HandbookSearch.Cli/
│       └── Program.cs
├── tests/
│   ├── HandbookSearch.Data.Tests/
│   └── HandbookSearch.Business.Tests/
├── AGENTS.md                  # AI assistant instructions
├── CLAUDE.md                  # Claude Code configuration
├── README.md                  # This file
└── HandbookSearch.sln
```

## Namespaces

All namespaces use `Olbrasoft.` prefix:
- `Olbrasoft.HandbookSearch.Data`
- `Olbrasoft.HandbookSearch.Data.EntityFrameworkCore`
- `Olbrasoft.HandbookSearch.Business`
- `Olbrasoft.HandbookSearch.AspNetCore.Api`
- `Olbrasoft.HandbookSearch.Cli`

## Contributing

See [engineering-handbook](https://github.com/Olbrasoft/engineering-handbook) for:
- [Git Workflow](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/workflow/git-workflow-workflow.md)
- [Testing Guide](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/dotnet/testing/index-testing.md)
- [SOLID Principles](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/dotnet/solid-principles/solid-principles.md)

## Issues

See [GitHub Issues](https://github.com/Olbrasoft/HandbookSearch/issues) for current development tasks.

## License

MIT License - see [LICENSE](LICENSE) file.
