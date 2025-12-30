namespace Olbrasoft.HandbookSearch.Translation.Cli.Configuration;

/// <summary>
/// Configuration options for Azure Translator service
/// </summary>
public class AzureTranslatorOptions
{
    public const string SectionName = "AzureTranslator";

    /// <summary>
    /// Azure Translator API key (primary account)
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Fallback Azure Translator API key (optional, used when primary fails)
    /// </summary>
    public string? FallbackApiKey { get; set; }

    /// <summary>
    /// Azure region (e.g., "westeurope")
    /// </summary>
    public required string Region { get; set; }

    /// <summary>
    /// API endpoint (default: https://api.cognitive.microsofttranslator.com)
    /// </summary>
    public string Endpoint { get; set; } = "https://api.cognitive.microsofttranslator.com";
}
