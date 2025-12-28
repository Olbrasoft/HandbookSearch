# AGENTS.md

Instructions for AI agents (Claude Code, GitHub Copilot, etc.) working with HandbookSearch repository.

## Project Overview

**HandbookSearch** - Semantic search engine for engineering-handbook using embeddings and PostgreSQL pgvector.

**Purpose:** Provide AI agents with relevant handbook documents based on natural language queries, eliminating the need to scan the entire handbook.

**Tech Stack:**
- .NET 10
- ASP.NET Core (Minimal API)
- PostgreSQL 16+ with pgvector extension
- Ollama with nomic-embed-text (768-dimensional embeddings)
- Entity Framework Core 10
- xUnit + Moq for testing

## Architecture

3-tier Clean Architecture:

```
API Layer (HandbookSearch.AspNetCore.Api)
    ↓
Business Layer (HandbookSearch.Business)
    ↓ Services: IEmbeddingService, ISearchService, IDocumentImportService
    ↓
Data Layer (HandbookSearch.Data + HandbookSearch.Data.EntityFrameworkCore)
    ↓ Document entity with Vector embedding
    ↓
PostgreSQL with pgvector
```

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Run tests
dotnet test

# Run specific test project
dotnet test tests/HandbookSearch.Business.Tests

# Run CLI
cd src/HandbookSearch.Cli
dotnet run -- import-all --path ~/GitHub/Olbrasoft/engineering-handbook

# Run API
cd src/HandbookSearch.AspNetCore.Api
dotnet run
```

## Important Paths

### Source Code
- `src/HandbookSearch.Data/` - Entities, DTOs, interfaces
- `src/HandbookSearch.Data.EntityFrameworkCore/` - DbContext, migrations
- `src/HandbookSearch.Business/` - Business logic and services
- `src/HandbookSearch.AspNetCore.Api/` - API endpoints
- `src/HandbookSearch.Cli/` - CLI import tool

### Tests
- `tests/HandbookSearch.Data.Tests/` - Data layer tests
- `tests/HandbookSearch.Business.Tests/` - Business logic tests

### Configuration
- `src/HandbookSearch.Cli/appsettings.json` - CLI configuration (no secrets)
- `src/HandbookSearch.AspNetCore.Api/appsettings.json` - API configuration (no secrets)
- `~/.microsoft/usersecrets/<id>/secrets.json` - User Secrets (local development)

## Namespaces

All projects use `Olbrasoft.` prefix:

- `Olbrasoft.HandbookSearch.Data` - Entities, DTOs
- `Olbrasoft.HandbookSearch.Data.EntityFrameworkCore` - EF Core, DbContext
- `Olbrasoft.HandbookSearch.Business` - Services, business logic
- `Olbrasoft.HandbookSearch.AspNetCore.Api` - API controllers/endpoints
- `Olbrasoft.HandbookSearch.Cli` - CLI commands

## Code Style

### General
- Follow [Microsoft C# Naming Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use file-scoped namespaces (`namespace Olbrasoft.HandbookSearch.Data;`)
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Target .NET 10 (`<TargetFramework>net10.0</TargetFramework>`)

### Testing
- **xUnit** for test framework (NOT NUnit)
- **Moq** for mocking (NOT NSubstitute)
- Test class naming: `{ClassUnderTest}Tests`
- Test method naming: `{MethodName}_{Scenario}_{ExpectedBehavior}`

Example:
```csharp
public class SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsResults()
    {
        // Arrange
        var mockDbContext = new Mock<HandbookSearchDbContext>();
        var service = new SearchService(mockDbContext.Object);

        // Act
        var results = await service.SearchAsync("git workflow");

        // Assert
        Assert.NotEmpty(results);
    }
}
```

### SOLID Principles
- **Single Responsibility** - One class, one purpose
- **Open/Closed** - Open for extension, closed for modification
- **Liskov Substitution** - Subtypes must be substitutable
- **Interface Segregation** - Small, focused interfaces
- **Dependency Inversion** - Depend on abstractions

See [SOLID Principles](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/dotnet/solid-principles/solid-principles.md)

## Dependencies

### External Services
- **PostgreSQL** (localhost:5432) - Database with pgvector extension
- **Ollama** (localhost:11434) - Embedding generation

### Key NuGet Packages
- `Npgsql.EntityFrameworkCore.PostgreSQL` - EF Core provider
- `Pgvector` - pgvector types for .NET
- `Pgvector.EntityFrameworkCore` - EF Core integration
- `Microsoft.Extensions.Http` - HttpClient factory
- `xUnit` - Testing framework
- `Moq` - Mocking library

## Secrets Management

**NEVER commit secrets to Git!**

### Development (User Secrets)

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=handbook_search;Username=postgres;Password=DevPass123"
dotnet user-secrets set "Ollama:BaseUrl" "http://localhost:11434"
```

### Production (Environment Variables)

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=handbook_search;Username=postgres;Password=ProdPass456"
export Ollama__BaseUrl="http://localhost:11434"
```

### GitHub Actions (Secrets)

Repository secrets:
- `POSTGRES_PASSWORD` - PostgreSQL password
- `OLLAMA_BASE_URL` - Ollama API URL

See [Secrets Management Guide](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/secrets-management.md)

## Database

### Connection String Format

**Development:**
```
Host=localhost;Database=handbook_search;Username=postgres;Password=xxx
```

**Production:**
```
Host=localhost;Database=handbook_search;Username=postgres;Password=xxx
```

### Migrations

```bash
# Add migration
cd src/HandbookSearch.Data.EntityFrameworkCore
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

### pgvector

Document entity uses `Vector` type for embeddings:

```csharp
[Column(TypeName = "vector(768)")]
public Vector? Embedding { get; set; }
```

HNSW index for cosine similarity search:
```csharp
entity.HasIndex(e => e.Embedding)
    .HasMethod("hnsw")
    .HasOperators("vector_cosine_ops");
```

## Common Tasks

### Add New Service

1. Create interface in `HandbookSearch.Business/IMyService.cs`
2. Implement in `HandbookSearch.Business/Services/MyService.cs`
3. Register in DI container (`Program.cs`):
   ```csharp
   builder.Services.AddScoped<IMyService, MyService>();
   ```
4. Write tests in `HandbookSearch.Business.Tests/MyServiceTests.cs`

### Add New Entity

1. Create entity in `HandbookSearch.Data/Entities/MyEntity.cs`
2. Add DbSet to `HandbookSearchDbContext.cs`
3. Configure in `OnModelCreating`
4. Create migration: `dotnet ef migrations add AddMyEntity`
5. Update database: `dotnet ef database update`

### Add New API Endpoint

1. Create controller in `HandbookSearch.AspNetCore.Api/Controllers/MyController.cs`
2. Inject required services via constructor
3. Add endpoint with proper HTTP verb attribute
4. Document with XML comments (for Swagger)
5. Add integration tests

## Git Workflow

- **Branches:** `feature/issue-N-description`, `fix/issue-N-description`
- **Commits:** Descriptive messages, commit frequently
- **Push:** After each significant change
- **PRs:** Create when feature is complete (after branch protection is enabled)

See [Git Workflow Guide](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/workflow/git-workflow-workflow.md)

## CI/CD

### Build Workflow (`.github/workflows/build.yml`)
- Triggers on: Push to main, PRs
- Steps: Restore → Build → Test

### Deploy Workflow (`.github/workflows/deploy.yml`)
- Triggers after: Build succeeds
- Runs on: Self-hosted runner (needs PostgreSQL + Ollama access)

See [CI/CD Guide](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/dotnet/continuous-deployment/local-apps-deploy-continuous-deployment.md)

## Reference Documentation

- [Engineering Handbook](https://github.com/Olbrasoft/engineering-handbook)
- [Project Setup](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/project-setup/repository-setup-project-setup.md)
- [Testing Guide](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/dotnet/testing/index-testing.md)
- [SOLID Principles](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/dotnet/solid-principles/solid-principles.md)

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Build fails | Run `dotnet restore` then `dotnet build` |
| Tests fail | Check connection string in User Secrets |
| Migrations fail | Verify PostgreSQL is running and accessible |
| Ollama errors | Check Ollama is running: `curl localhost:11434/api/tags` |
| pgvector errors | Ensure extension is enabled: `CREATE EXTENSION vector;` |

## Notes for AI Agents

- This project follows Olbrasoft conventions from engineering-handbook
- Use **xUnit + Moq** for testing (NOT NUnit/NSubstitute)
- All configuration secrets go in User Secrets or Environment Variables
- Never commit `appsettings.*.local.json` or `.env` files
- Self-hosted runner required for deployment (needs local PostgreSQL + Ollama)
- Embedding dimension is 768 (nomic-embed-text model)
- Use cosine distance for similarity search (`CosineDistance()` in EF Core)
