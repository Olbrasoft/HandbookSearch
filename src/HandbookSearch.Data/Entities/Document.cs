using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace Olbrasoft.HandbookSearch.Data.Entities;

public class Document
{
    public int Id { get; set; }

    /// <summary>
    /// Relative path from handbook root (e.g., "development-guidelines/workflow-guide.md")
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// Document title extracted from first H1
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Full markdown content
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Content hash for change detection (SHA256)
    /// </summary>
    public required string ContentHash { get; set; }

    /// <summary>
    /// Language code (e.g., "en", "cs")
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// 768-dimensional embedding from nomic-embed-text
    /// </summary>
    [Column(TypeName = "vector(768)")]
    public Vector? Embedding { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
