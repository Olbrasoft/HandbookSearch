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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with statistics</returns>
    Task<ImportResult> ImportAllAsync(string handbookPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a single markdown file
    /// </summary>
    /// <param name="filePath">Full path to the markdown file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file was imported/updated, false if skipped</returns>
    Task<bool> ImportFileAsync(string filePath, CancellationToken cancellationToken = default);
}
