namespace Olbrasoft.HandbookSearch.Business;

/// <summary>
/// Service for generating text embeddings for semantic search
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates a 768-dimensional embedding vector for the given text
    /// </summary>
    /// <param name="text">Text to generate embedding for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>768-dimensional float array representing the text embedding</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
