namespace Olbrasoft.HandbookSearch.Business;

/// <summary>
/// Service for importing markdown documents from engineering-handbook into the database
/// </summary>
public interface IDocumentImportService
{
    /// <summary>
    /// Imports all markdown files from the specified handbook directory
    /// </summary>
    /// <param name="handbookPath">Root path of the engineering handbook</param>
    /// <param name="language">Language code (e.g., "en", "cs"). Defaults to "en".</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with statistics</returns>
    Task<ImportResult> ImportAllAsync(string handbookPath, string language = "en", CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a single markdown file
    /// </summary>
    /// <param name="filePath">Full path to the markdown file</param>
    /// <param name="language">Language code (e.g., "en", "cs"). Defaults to "en".</param>
    /// <param name="handbookPath">Root path of the handbook directory (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file was imported/updated, false if skipped</returns>
    Task<bool> ImportFileAsync(string filePath, string language = "en", string? handbookPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from the database by its relative file path
    /// </summary>
    /// <param name="relativePath">Relative path from handbook root (e.g., "docs/guide.md")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if document was deleted, false if not found</returns>
    Task<bool> DeleteDocumentAsync(string relativePath, CancellationToken cancellationToken = default);
}
