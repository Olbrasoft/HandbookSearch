namespace Olbrasoft.HandbookSearch.Business;

/// <summary>
/// Result of document import operation with statistics
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Number of new documents added to the database
    /// </summary>
    public int Added { get; set; }

    /// <summary>
    /// Number of existing documents updated due to content changes
    /// </summary>
    public int Updated { get; set; }

    /// <summary>
    /// Number of documents skipped (no changes detected)
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Total number of documents processed
    /// </summary>
    public int Total => Added + Updated + Skipped;

    /// <summary>
    /// List of errors encountered during import (file path + error message)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether the import completed without any errors
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}
