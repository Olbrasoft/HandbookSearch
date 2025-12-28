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

        var request = new OllamaEmbeddingRequest
        {
            Model = _options.Model,
            Prompt = text
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
