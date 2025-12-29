namespace Olbrasoft.HandbookSearch.Translation.Cli.Services;

/// <summary>
/// Service for translating text using Azure Translator API
/// </summary>
public interface IAzureTranslatorService
{
    /// <summary>
    /// Translates text from source language to target language
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="targetLanguage">Target language code (e.g., "cs", "de")</param>
    /// <param name="sourceLanguage">Source language code (auto-detected if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Translated text</returns>
    Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default);
}
