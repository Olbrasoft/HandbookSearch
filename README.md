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
| Web UI | `HandbookSearch.Web` | Simple HTML/JS search interface |
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

The database `handbook_search` with pgvector extension has been created and is ready to use.

**Verify database:**
```bash
psql -U postgres -d handbook_search -c "SELECT extname, extversion FROM pg_extension WHERE extname = 'vector';"
```

**Run migrations** (when implemented):
```bash
cd src/HandbookSearch.Data.EntityFrameworkCore
dotnet ef database update
```

### Secrets Management

HandbookSearch uses [SecureStore](https://github.com/neosmart/SecureStore) for encrypted secrets storage.

#### Initial Setup

1. Install SecureStore CLI:
   ```bash
   dotnet tool install --global SecureStore.Client
   ```

2. Create vault and keyfile:
   ```bash
   mkdir -p ~/.config/handbook-search/secrets
   mkdir -p ~/.config/handbook-search/keys

   SecureStore create \
     -s ~/.config/handbook-search/secrets/secrets.json \
     -k ~/.config/handbook-search/keys/secrets.key

   chmod 600 ~/.config/handbook-search/keys/secrets.key
   ```

3. Add secrets:
   ```bash
   SECRETS=~/.config/handbook-search/secrets/secrets.json
   KEY=~/.config/handbook-search/keys/secrets.key

   # Database password (if needed for production)
   SecureStore set -s $SECRETS -k $KEY "Database:Password=your_password"

   # Azure Translator (for Czech embeddings)
   SecureStore set -s $SECRETS -k $KEY "AzureTranslator:ApiKey=your_api_key"
   ```

#### Secrets Reference

| Secret | Description | Required |
|--------|-------------|----------|
| `Database:Password` | PostgreSQL password | Production only |
| `AzureTranslator:ApiKey` | Azure Translator API key | For Czech embeddings |
| `AzureTranslator:FallbackApiKey` | Fallback API key | Optional |

**Note:** Local PostgreSQL typically runs without password. Only set `Database:Password` if your setup requires authentication.

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

### Web UI

**Start the web interface:**

```bash
# Terminal 1: Start API server
cd src/HandbookSearch.AspNetCore.Api
dotnet run

# Terminal 2: Start web UI
cd src/HandbookSearch.Web
python3 -m http.server 3000
```

**Open in browser:**
```
http://localhost:3000
```

**Features:**
- Search in Czech or English
- Clickable links to GitHub files
- Color-coded similarity scores
- Copy file paths to clipboard
- Responsive design

See [Web UI README](src/HandbookSearch.Web/README.md) for details.

### CLI Commands

```bash
# Import all markdown files
dotnet run -- import-all --path ~/GitHub/Olbrasoft/engineering-handbook

# Import specific files
dotnet run -- import-files --files "workflow/git-workflow.md,testing/unit-testing.md"
```

### Automatic Updates

**GitHub Actions workflow** automatically updates embeddings when markdown files change in engineering-handbook:

📍 **Workflow Location:** [engineering-handbook/.github/workflows/update-embeddings.yml](https://github.com/Olbrasoft/engineering-handbook/blob/main/.github/workflows/update-embeddings.yml)

**How it works:**
1. Push markdown changes to engineering-handbook `main` branch
2. Workflow detects changed `.md` files using [tj-actions/changed-files](https://github.com/tj-actions/changed-files)
3. Automatically imports changed files to HandbookSearch database
4. Embeddings stay in sync with handbook content

**Requirements:**
- Self-hosted GitHub Actions runner with label `handbook-search`
- Runner must have access to local PostgreSQL and Ollama
- HandbookSearch.Cli built in Release mode

**Setup Instructions:** See [engineering-handbook/.github/RUNNER_SETUP.md](https://github.com/Olbrasoft/engineering-handbook/blob/main/.github/RUNNER_SETUP.md)

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
