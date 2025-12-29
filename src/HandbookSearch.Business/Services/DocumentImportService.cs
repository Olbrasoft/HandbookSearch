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
    private readonly ITranslationService? _translationService;

    public DocumentImportService(
        HandbookSearchDbContext dbContext,
        IEmbeddingService embeddingService,
        ITranslationService? translationService = null)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _translationService = translationService;
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
                var imported = await ImportFileAsync(filePath, language, handbookPath, translateCs: false, cancellationToken);
                if (imported)
                {
                    // For English files: check if new or updated
                    // For Czech files: always counted as updated (updating existing English records)
                    if (language == "en")
                    {
                        var relativePath = Path.GetRelativePath(handbookPath, filePath);
                        var existingDoc = await _dbContext.Documents
                            .AsNoTracking()
                            .FirstOrDefaultAsync(d => d.FilePath == relativePath, cancellationToken);

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
                        result.Updated++;
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
    public async Task<bool> ImportFileAsync(string filePath, string language = "en", string? handbookPath = null, bool translateCs = false, CancellationToken cancellationToken = default)
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

        // Extract relative path
        string relativePath;
        if (!string.IsNullOrWhiteSpace(handbookPath))
        {
            // Use provided handbook path to calculate relative path
            relativePath = Path.GetRelativePath(handbookPath, filePath);
        }
        else
        {
            // Fallback: try to extract from file path
            relativePath = Path.GetFileName(filePath);
            if (filePath.Contains("engineering-handbook"))
            {
                var handbookIndex = filePath.IndexOf("engineering-handbook", StringComparison.OrdinalIgnoreCase);
                var detectedHandbookPath = filePath.Substring(0, handbookIndex + "engineering-handbook".Length);
                relativePath = Path.GetRelativePath(detectedHandbookPath, filePath);
            }
        }

        // Find existing document by FilePath only (one record per English file)
        var existingDoc = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.FilePath == relativePath, cancellationToken);

        if (language == "cs")
        {
            // Czech import: Update existing English record with Czech embedding
            if (existingDoc == null)
            {
                throw new InvalidOperationException(
                    $"Cannot import Czech embedding for '{relativePath}': English document not found. " +
                    "Import English documents first.");
            }

            // Generate Czech embedding
            var embeddingArray = await _embeddingService.GenerateEmbeddingAsync(content, cancellationToken);
            var embedding = new Vector(embeddingArray);

            // Update Czech embedding
            existingDoc.EmbeddingCs = embedding;
            existingDoc.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        // English import: Create or update English document with English embedding
        if (existingDoc != null && existingDoc.ContentHash == contentHash)
        {
            // No changes, skip
            return false;
        }

        // Extract title from first H1 heading
        var title = ExtractTitle(content);

        // Generate English embedding
        var englishEmbeddingArray = await _embeddingService.GenerateEmbeddingAsync(content, cancellationToken);
        var englishEmbedding = new Vector(englishEmbeddingArray);

        // Generate Czech embedding if requested
        Vector? czechEmbedding = null;
        if (translateCs && _translationService != null)
        {
            // Translate to Czech IN MEMORY (not saved to disk)
            var czechContent = await _translationService.TranslateToCzechAsync(content, cancellationToken);

            // Generate Czech embedding
            var czechEmbeddingArray = await _embeddingService.GenerateEmbeddingAsync(czechContent, cancellationToken);
            czechEmbedding = new Vector(czechEmbeddingArray);

            // Czech translation is discarded here - only embedding is stored
        }

        if (existingDoc != null)
        {
            // Update existing English document
            existingDoc.Content = content;
            existingDoc.ContentHash = contentHash;
            existingDoc.Title = title;
            existingDoc.Embedding = englishEmbedding;
            existingDoc.EmbeddingCs = czechEmbedding; // Null if translateCs=false
            existingDoc.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Add new English document
            var newDoc = new Document
            {
                FilePath = relativePath,
                Title = title,
                Content = content,
                ContentHash = contentHash,
                Embedding = englishEmbedding,
                EmbeddingCs = czechEmbedding // Null if translateCs=false
            };

            _dbContext.Documents.Add(newDoc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.FilePath == relativePath, cancellationToken);

        if (document == null)
            return false;

        _dbContext.Documents.Remove(document);
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
