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
    /// 1024-dimensional English embedding from qwen3-embedding:0.6b
    /// </summary>
    [Column(TypeName = "vector(1024)")]
    public Vector? Embedding { get; set; }

    /// <summary>
    /// 1024-dimensional Czech embedding from qwen3-embedding:0.6b
    /// </summary>
    [Column(TypeName = "vector(1024)")]
    public Vector? EmbeddingCs { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
