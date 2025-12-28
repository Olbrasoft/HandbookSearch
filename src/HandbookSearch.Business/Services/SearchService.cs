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

        // 2. Search using cosine distance
        var resultsQuery = _dbContext.Documents
            .Where(d => d.Embedding != null)
            .Select(d => new
            {
                Document = d,
                Distance = d.Embedding!.CosineDistance(queryVector)
            });

        // 3. Optional: filter by max distance
        if (maxDistance.HasValue)
        {
            resultsQuery = resultsQuery.Where(x => x.Distance < maxDistance.Value);
        }

        // 4. Execute query
        var results = await resultsQuery
            .OrderBy(x => x.Distance)
            .Take(limit)
            .ToListAsync(ct);

        // 5. Map to SearchResult
        return results.Select(r => new SearchResult(
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
