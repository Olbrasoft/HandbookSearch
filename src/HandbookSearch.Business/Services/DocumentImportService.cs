using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Olbrasoft.HandbookSearch.Data.Entities;
using Olbrasoft.HandbookSearch.Data.EntityFrameworkCore;
using Pgvector;

namespace Olbrasoft.HandbookSearch.Business.Services;

/// <summary>
/// Service for importing markdown documents from engineering-handbook into the database
/// </summary>
public class DocumentImportService : IDocumentImportService
{
    private readonly HandbookSearchDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;

    public DocumentImportService(
        HandbookSearchDbContext dbContext,
        IEmbeddingService embeddingService)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
    }

    /// <inheritdoc />
    public async Task<ImportResult> ImportAllAsync(string handbookPath, string language = "en", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handbookPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);

        if (!Directory.Exists(handbookPath))
        {
            throw new DirectoryNotFoundException($"Handbook directory not found: {handbookPath}");
        }

        var result = new ImportResult();
        var markdownFiles = Directory.GetFiles(handbookPath, "*.md", SearchOption.AllDirectories);

        foreach (var filePath in markdownFiles)
        {
            try
            {
                var imported = await ImportFileAsync(filePath, language, cancellationToken);
                if (imported)
                {
                    // Check if it was an update or new document
                    var relativePath = Path.GetRelativePath(handbookPath, filePath);
                    var existingDoc = await _dbContext.Documents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.FilePath == relativePath && d.Language == language, cancellationToken);

                    if (existingDoc != null)
                    {
                        result.Updated++;
                    }
                    else
                    {
                        result.Added++;
                    }
                }
                else
                {
                    result.Skipped++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{filePath}: {ex.Message}");
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> ImportFileAsync(string filePath, string language = "en", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        // Read file content
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var contentHash = CalculateSHA256Hash(content);

        // Extract relative path (assuming we're always in a handbook directory structure)
        var relativePath = Path.GetFileName(filePath);
        if (filePath.Contains("engineering-handbook"))
        {
            var handbookIndex = filePath.IndexOf("engineering-handbook", StringComparison.OrdinalIgnoreCase);
            var handbookPath = filePath.Substring(0, handbookIndex + "engineering-handbook".Length);
            relativePath = Path.GetRelativePath(handbookPath, filePath);
        }

        // Check if document exists for this language and hash changed
        var existingDoc = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.FilePath == relativePath && d.Language == language, cancellationToken);

        if (existingDoc != null && existingDoc.ContentHash == contentHash)
        {
            // No changes, skip
            return false;
        }

        // Extract title from first H1 heading
        var title = ExtractTitle(content);

        // Generate embedding
        var embeddingArray = await _embeddingService.GenerateEmbeddingAsync(content, cancellationToken);
        var embedding = new Vector(embeddingArray);

        if (existingDoc != null)
        {
            // Update existing document
            existingDoc.Content = content;
            existingDoc.ContentHash = contentHash;
            existingDoc.Title = title;
            existingDoc.Embedding = embedding;
            existingDoc.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Add new document
            var newDoc = new Document
            {
                FilePath = relativePath,
                Title = title,
                Content = content,
                ContentHash = contentHash,
                Language = language,
                Embedding = embedding
            };

            _dbContext.Documents.Add(newDoc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string CalculateSHA256Hash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? ExtractTitle(string content)
    {
        // Find first H1 heading (# Title)
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("# "))
            {
                return line.Substring(2).Trim();
            }
        }

        return null;
    }
}
