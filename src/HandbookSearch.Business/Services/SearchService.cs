using Microsoft.EntityFrameworkCore;
using Olbrasoft.HandbookSearch.Data.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Olbrasoft.HandbookSearch.Business.Services;

public class SearchService : ISearchService
{
    private readonly HandbookSearchDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;

    public SearchService(
        HandbookSearchDbContext dbContext,
        IEmbeddingService embeddingService)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
    }

    public async Task<List<SearchResult>> SearchAsync(
        string query,
        int limit = 5,
        float? maxDistance = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query, nameof(query));

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than 0");
        }

        // 1. Generate embedding for query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, ct);
        var queryVector = new Vector(queryEmbedding);

        // 2. Search in English embeddings
        var englishResultsQuery = _dbContext.Documents
            .Where(d => d.Embedding != null)
            .Select(d => new
            {
                Document = d,
                Distance = d.Embedding!.CosineDistance(queryVector)
            });

        if (maxDistance.HasValue)
        {
            englishResultsQuery = englishResultsQuery.Where(x => x.Distance < maxDistance.Value);
        }

        var englishResults = await englishResultsQuery.ToListAsync(ct);

        // 3. Search in Czech embeddings
        var czechResultsQuery = _dbContext.Documents
            .Where(d => d.EmbeddingCs != null)
            .Select(d => new
            {
                Document = d,
                Distance = d.EmbeddingCs!.CosineDistance(queryVector)
            });

        if (maxDistance.HasValue)
        {
            czechResultsQuery = czechResultsQuery.Where(x => x.Distance < maxDistance.Value);
        }

        var czechResults = await czechResultsQuery.ToListAsync(ct);

        // 4. Merge results and deduplicate by FilePath (keep best match)
        var allResults = englishResults.Concat(czechResults)
            .GroupBy(r => r.Document.FilePath)
            .Select(g => g.OrderBy(r => r.Distance).First())
            .OrderBy(r => r.Distance)
            .Take(limit)
            .ToList();

        // 5. Map to SearchResult
        return allResults.Select(r => new SearchResult(
            DocumentId: r.Document.Id,
            FilePath: r.Document.FilePath,
            Title: r.Document.Title,
            ContentSnippet: GetSnippet(r.Document.Content, 200),
            Distance: (float)r.Distance
        )).ToList();
    }

    private static string GetSnippet(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        if (content.Length <= maxLength) return content;
        return content[..maxLength] + "...";
    }
}
