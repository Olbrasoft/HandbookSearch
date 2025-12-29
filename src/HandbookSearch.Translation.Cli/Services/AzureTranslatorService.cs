using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olbrasoft.HandbookSearch.Translation.Cli.Configuration;

namespace Olbrasoft.HandbookSearch.Translation.Cli.Services;

/// <summary>
/// Azure Translator service implementation with rate limiting
/// </summary>
public class AzureTranslatorService : IAzureTranslatorService
{
    private readonly HttpClient _httpClient;
    private readonly AzureTranslatorOptions _options;
    private readonly ILogger<AzureTranslatorService> _logger;
    private const int CharsPerSecond = 555; // Azure rate limit

    public AzureTranslatorService(
        HttpClient httpClient,
        IOptions<AzureTranslatorOptions> options,
        ILogger<AzureTranslatorService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_options.Endpoint);
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", _options.Region);
    }

    public async Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguage);

        // Calculate delay based on text length
        var delay = CalculateDelay(text.Length);

        _logger.LogInformation(
            "Translating {CharCount} characters to {TargetLang}, delay: {Delay}ms",
            text.Length,
            targetLanguage,
            delay.TotalMilliseconds);

        // Apply rate limiting delay BEFORE API call
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }

        // Build request URL
        var route = $"/translate?api-version=3.0&to={targetLanguage}";
        if (!string.IsNullOrEmpty(sourceLanguage))
        {
            route += $"&from={sourceLanguage}";
        }

        // Build request body
        var requestBody = new[] { new { Text = text } };
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
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<TranslationResponse[]>(responseContent, options);

        if (result == null || result.Length == 0 || result[0].Translations.Length == 0)
        {
            throw new InvalidOperationException("Translation API returned empty result");
        }

        return result[0].Translations[0].Text;
    }

    private static TimeSpan CalculateDelay(int characterCount)
    {
        // Formula from engineering handbook:
        // seconds = characterCount / 555 (chars per second)
        // Add 20% safety margin
        var seconds = (double)characterCount / CharsPerSecond;
        seconds *= 1.2; // 20% safety margin
        return TimeSpan.FromSeconds(Math.Max(0.2, seconds)); // Minimum 200ms
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
