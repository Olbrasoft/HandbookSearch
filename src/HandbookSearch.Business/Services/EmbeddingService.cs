using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Olbrasoft.HandbookSearch.Business.Services;

/// <summary>
/// Service for generating embeddings using Ollama API
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public EmbeddingService(HttpClient httpClient, IOptions<OllamaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        // Truncate text if too long (nomic-embed-text has 2048 token context length)
        // Approx 4 chars per token, so 6000 chars ~= 1500 tokens (safe margin)
        const int maxChars = 6000;
        var truncatedText = text.Length > maxChars ? text[..maxChars] : text;

        var request = new OllamaEmbeddingRequest
        {
            Model = _options.Model,
            Prompt = truncatedText
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_options.BaseUrl}/api/embeddings",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize Ollama response");

        if (result.Embedding == null || result.Embedding.Length != _options.Dimensions)
        {
            throw new InvalidOperationException(
                $"Expected {_options.Dimensions} dimensions, got {result.Embedding?.Length ?? 0}");
        }

        return result.Embedding;
    }

    private record OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; init; }
    }

    private record OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; init; }
    }
}
