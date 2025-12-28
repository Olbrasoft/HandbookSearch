namespace Olbrasoft.HandbookSearch.Business;

/// <summary>
/// Configuration options for Ollama integration
/// </summary>
public class OllamaOptions
{
    /// <summary>
    /// Ollama API base URL (default: http://localhost:11434)
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Embedding model name (default: nomic-embed-text)
    /// </summary>
    public string Model { get; set; } = "nomic-embed-text";

    /// <summary>
    /// Expected embedding dimensions (default: 768 for nomic-embed-text)
    /// </summary>
    public int Dimensions { get; set; } = 768;
}
