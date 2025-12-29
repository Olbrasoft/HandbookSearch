using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Olbrasoft.HandbookSearch.Business.Services;

/// <summary>
/// Azure Translator service implementation with rate limiting
/// Translates English markdown to Czech for embedding generation (in-memory only)
/// </summary>
public class AzureTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly AzureTranslatorOptions _options;
    private readonly ILogger<AzureTranslationService> _logger;

    // Sliding window rate limiter state
    private readonly Queue<(DateTime timestamp, int charCount)> _recentTranslations = new();
    private readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);
    private readonly int _maxCharsPerMinute = 33000; // Conservative limit (Azure: ~33,300 chars/min)
    private readonly SemaphoreSlim _rateLimitLock = new(1, 1);

    public AzureTranslationService(
        HttpClient httpClient,
        IOptions<AzureTranslatorOptions> options,
        ILogger<AzureTranslationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_options.Endpoint);
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", _options.Region);
    }

    /// <inheritdoc />
    public async Task<string> TranslateToCzechAsync(string markdownContent, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markdownContent);

        var charCount = markdownContent.Length;

        _logger.LogInformation("Translating {CharCount} characters to Czech", charCount);

        // Apply sliding window rate limiting
        await ApplyRateLimitAsync(charCount, cancellationToken);

        // Build request URL with textType=html to preserve Markdown formatting
        var route = "/translate?api-version=3.0&from=en&to=cs&textType=html";

        // Build request body
        var requestBody = new[] { new { Text = markdownContent } };
        using var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        // Send request
        var response = await _httpClient.PostAsync(route, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Translation failed: {StatusCode} - {Error}",
                response.StatusCode,
                errorContent);

            throw new HttpRequestException(
                $"Translation failed with status {response.StatusCode}: {errorContent}");
        }

        // Parse response
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<TranslationResponse[]>(responseContent, jsonOptions);

        if (result == null || result.Length == 0 || result[0].Translations.Length == 0)
        {
            throw new InvalidOperationException("Translation API returned empty result");
        }

        // Track this translation for rate limiting
        await _rateLimitLock.WaitAsync(cancellationToken);
        try
        {
            _recentTranslations.Enqueue((DateTime.UtcNow, charCount));
        }
        finally
        {
            _rateLimitLock.Release();
        }

        return result[0].Translations[0].Text;
    }

    private async Task ApplyRateLimitAsync(int charCount, CancellationToken cancellationToken)
    {
        await _rateLimitLock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;

            // Remove old entries outside the rate limit window
            while (_recentTranslations.Count > 0 && (now - _recentTranslations.Peek().timestamp) > _rateLimitWindow)
            {
                _recentTranslations.Dequeue();
            }

            // Calculate characters in current window
            var charsInWindow = _recentTranslations.Sum(x => x.charCount);

            // Wait if we would exceed rate limit
            if (charsInWindow + charCount > _maxCharsPerMinute)
            {
                var oldestInWindow = _recentTranslations.Peek().timestamp;
                var waitTime = _rateLimitWindow - (now - oldestInWindow) + TimeSpan.FromSeconds(5); // +5s safety margin

                _logger.LogWarning(
                    "Rate limit reached: {CharsInWindow}/{MaxChars} chars. Waiting {WaitSeconds}s",
                    charsInWindow,
                    _maxCharsPerMinute,
                    (int)waitTime.TotalSeconds);

                await Task.Delay(waitTime, cancellationToken);

                // Clear old entries after waiting
                now = DateTime.UtcNow;
                while (_recentTranslations.Count > 0 && (now - _recentTranslations.Peek().timestamp) > _rateLimitWindow)
                {
                    _recentTranslations.Dequeue();
                }
            }
        }
        finally
        {
            _rateLimitLock.Release();
        }
    }

    private class TranslationResponse
    {
        public required Translation[] Translations { get; set; }
    }

    private class Translation
    {
        public required string Text { get; set; }
        public required string To { get; set; }
    }
}

/// <summary>
/// Configuration options for Azure Translator service
/// </summary>
public class AzureTranslatorOptions
{
    public const string SectionName = "AzureTranslator";

    /// <summary>
    /// Azure Translator API key
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Azure region (e.g., "westeurope")
    /// </summary>
    public required string Region { get; set; }

    /// <summary>
    /// API endpoint (default: https://api.cognitive.microsofttranslator.com)
    /// </summary>
    public string Endpoint { get; set; } = "https://api.cognitive.microsofttranslator.com";
}
