namespace Olbrasoft.HandbookSearch.Business;

public interface ISearchService
{
    Task<List<SearchResult>> SearchAsync(
        string query,
        int limit = 5,
        float? maxDistance = null,
        CancellationToken ct = default);
}

public record SearchResult(
    int DocumentId,
    string FilePath,
    string? Title,
    string ContentSnippet,
    float Distance
);
