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

        // 2. Search in BOTH English and Czech embeddings
        var allDocuments = await _dbContext.Documents
            .Where(d => d.Embedding != null || d.EmbeddingCs != null)
            .Select(d => new
            {
                Document = d,
                DistanceEn = d.Embedding != null ? (double?)d.Embedding.CosineDistance(queryVector) : null,
                DistanceCs = d.EmbeddingCs != null ? (double?)d.EmbeddingCs.CosineDistance(queryVector) : null
            })
            .ToListAsync(ct);

        // 3. Merge results: take best (minimum) distance from both embeddings
        var mergedResults = allDocuments
            .Select(d => new
            {
                d.Document,
                BestDistance = new[] { d.DistanceEn, d.DistanceCs }
                    .Where(dist => dist.HasValue)
                    .Min(dist => dist!.Value)
            })
            .Where(x => !maxDistance.HasValue || x.BestDistance < maxDistance.Value)
            .OrderBy(x => x.BestDistance)
            .Take(limit)
            .ToList();

        // 4. Map to SearchResult
        return mergedResults.Select(r => new SearchResult(
            DocumentId: r.Document.Id,
            FilePath: r.Document.FilePath,
            Title: r.Document.Title,
            ContentSnippet: GetSnippet(r.Document.Content, 200),
            Distance: (float)r.BestDistance
        )).ToList();
    }

    private static string GetSnippet(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        if (content.Length <= maxLength) return content;
        return content[..maxLength] + "...";
    }
}
