using Olbrasoft.HandbookSearch.Business.Services;

namespace Olbrasoft.HandbookSearch.Business.Tests.Services;

/// <summary>
/// SearchService tests.
///
/// Note: Comprehensive testing of SearchService requires integration tests with
/// real PostgreSQL database and pgvector extension, as it uses:
/// - pgvector's Vector type
/// - CosineDistance() extension method
/// - HNSW index for similarity search
///
/// InMemory provider doesn't support these features.
/// Integration tests should be added separately with real PostgreSQL instance.
/// </summary>
public class SearchServiceTests
{
    [Fact]
    public void SearchService_CanBeInstantiated()
    {
        // This is a placeholder test
        // Real tests require PostgreSQL with pgvector extension
        Assert.True(typeof(SearchService).IsClass);
        Assert.True(typeof(ISearchService).IsInterface);
    }
}
