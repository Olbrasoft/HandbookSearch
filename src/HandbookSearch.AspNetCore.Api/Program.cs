using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Olbrasoft.HandbookSearch.Business;
using Olbrasoft.HandbookSearch.Business.Services;
using Olbrasoft.HandbookSearch.Data.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<HandbookSearchDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));

// HTTP Client for EmbeddingService
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>();

// Business Services
builder.Services.AddScoped<ISearchService, SearchService>();

// CORS for local development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React, Vite
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HandbookSearch API",
        Version = "v1",
        Description = "Semantic search API for engineering handbook documents"
    });
});

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HandbookSearch API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseCors();

// Search endpoint
app.MapGet("/api/search", async (
    string q,
    ISearchService searchService,
    int limit = 5,
    float? maxDistance = null,
    CancellationToken ct = default) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest(new { error = "Query parameter 'q' is required" });
    }

    if (limit <= 0 || limit > 100)
    {
        return Results.BadRequest(new { error = "Limit must be between 1 and 100" });
    }

    try
    {
        var results = await searchService.SearchAsync(q, limit, maxDistance, ct);

        return Results.Ok(new
        {
            query = q,
            limit,
            maxDistance,
            results = results.Select(r => new
            {
                filePath = r.FilePath,
                title = r.Title,
                snippet = r.ContentSnippet,
                distance = r.Distance,
                score = 1.0f - r.Distance // Convert distance to similarity score (0-1)
            })
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Search failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("SearchDocuments")
.WithDescription("Search handbook documents by semantic similarity")
.WithTags("Search")
.Produces<object>(200)
.Produces<object>(400)
.Produces<object>(500);

app.Run();

// Make Program class available to integration tests
public partial class Program { }
