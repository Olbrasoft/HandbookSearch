namespace Olbrasoft.HandbookSearch.Business;

/// <summary>
/// Service for translating markdown content to Czech
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translates English markdown content to Czech
    /// </summary>
    /// <param name="markdownContent">English markdown content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Czech translated markdown (preserves formatting via textType=html)</returns>
    Task<string> TranslateToCzechAsync(string markdownContent, CancellationToken cancellationToken = default);
}
