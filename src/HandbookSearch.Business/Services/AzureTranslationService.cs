using System.Net;
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

        // Configure HttpClient base address only (headers set per-request for fallback support)
        _httpClient.BaseAddress = new Uri(_options.Endpoint);
    }

    /// <inheritdoc />
    public async Task<string> TranslateToCzechAsync(string markdownContent, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markdownContent);

        var charCount = markdownContent.Length;

        _logger.LogInformation("Translating {CharCount} characters to Czech", charCount);

        // Apply sliding window rate limiting BEFORE translation
        await ApplyRateLimitAsync(charCount, cancellationToken);

        // Translate with automatic fallback support
        var translatedText = await TranslateWithFallbackAsync(
            markdownContent,
            targetLanguage: "cs",
            sourceLanguage: "en",
            cancellationToken);

        // Track this translation for rate limiting AFTER successful translation
        await _rateLimitLock.WaitAsync(cancellationToken);
        try
        {
            _recentTranslations.Enqueue((DateTime.UtcNow, charCount));
        }
        finally
        {
            _rateLimitLock.Release();
        }

        return translatedText;
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

    /// <summary>
    /// Translates text with automatic fallback to secondary API key on failure
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="targetLanguage">Target language code</param>
    /// <param name="sourceLanguage">Optional source language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Translated text</returns>
    /// <exception cref="HttpRequestException">Thrown when both primary and fallback accounts fail</exception>
    private async Task<string> TranslateWithFallbackAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage,
        CancellationToken cancellationToken)
    {
        // Try primary API key
        try
        {
            return await SendTranslationRequestAsync(
                text, targetLanguage, sourceLanguage,
                _options.ApiKey, "primary account", cancellationToken);
        }
        catch (HttpRequestException ex) when (IsFallbackEligible(ex))
        {
            if (string.IsNullOrEmpty(_options.FallbackApiKey))
            {
                _logger.LogError(ex,
                    "Primary account failed: {StatusCode}, but no fallback key configured",
                    ex.StatusCode);
                throw;
            }

            _logger.LogError(ex,
                "Primary account failed with {StatusCode}. Retrying with fallback account...",
                ex.StatusCode);

            // Try fallback API key
            try
            {
                var result = await SendTranslationRequestAsync(
                    text, targetLanguage, sourceLanguage,
                    _options.FallbackApiKey, "fallback account", cancellationToken);

                _logger.LogInformation("Translation completed successfully using fallback account");
                return result;
            }
            catch (HttpRequestException fallbackEx) when (IsFallbackEligible(fallbackEx))
            {
                _logger.LogCritical(
                    "Both accounts failed. Primary: {PrimaryError}, Fallback: {FallbackError}",
                    ex.StatusCode, fallbackEx.StatusCode);

                var nextReset = GetNextQuotaResetDate();
                throw new HttpRequestException(
                    $"Both accounts failed. Primary ({ex.StatusCode}): {ex.Message}, " +
                    $"Fallback ({fallbackEx.StatusCode}): {fallbackEx.Message}. " +
                    $"Quota resets on {nextReset:yyyy-MM-dd}.", fallbackEx);
            }
            catch (Exception fallbackEx) when (fallbackEx is TaskCanceledException or OperationCanceledException)
            {
                _logger.LogWarning(fallbackEx, "Fallback translation was cancelled");
                throw;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback translation failed with unexpected error");
                throw;
            }
        }
    }

    /// <summary>
    /// Sends translation request to Azure Translator API using specified API key
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="targetLanguage">Target language code</param>
    /// <param name="sourceLanguage">Optional source language code</param>
    /// <param name="apiKey">Azure Translator API key to use</param>
    /// <param name="accountDescription">Account identifier for logging (e.g., "primary account")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Translated text</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails</exception>
    /// <exception cref="InvalidOperationException">Thrown when API returns empty result</exception>
    private async Task<string> SendTranslationRequestAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage,
        string apiKey,
        string accountDescription,
        CancellationToken cancellationToken)
    {
        // Build request URL with textType=html to preserve Markdown formatting
        var route = $"/translate?api-version=3.0&to={targetLanguage}&textType=html";
        if (!string.IsNullOrEmpty(sourceLanguage))
        {
            route += $"&from={sourceLanguage}";
        }

        // Build request
        using var request = new HttpRequestMessage(HttpMethod.Post, route);

        // Set headers per-request (not in constructor) to support different API keys
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        request.Headers.Add("Ocp-Apim-Subscription-Region", _options.Region);

        // Build request body
        var requestBody = new[] { new { Text = text } };
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        // Send request
        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Log Retry-After header if present (429 errors)
            if (response.StatusCode == HttpStatusCode.TooManyRequests &&
                response.Headers.TryGetValues("Retry-After", out var retryAfterValues))
            {
                var retryAfter = retryAfterValues.First();
                _logger.LogWarning(
                    "Rate limited ({Account}). Retry after: {RetryAfter} seconds",
                    accountDescription, retryAfter);
            }

            // Try to parse Azure error response
            var errorDetails = TryParseErrorDetails(errorContent);

            _logger.LogError(
                "Translation failed ({Account}): {StatusCode} - Code: {ErrorCode}, Message: {ErrorMessage}",
                accountDescription, response.StatusCode, errorDetails.Code, errorDetails.Message);

            throw new HttpRequestException(
                $"Translation failed ({accountDescription}) with status {response.StatusCode}: {errorDetails.Message}",
                null,
                response.StatusCode);
        }

        // Parse response
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<TranslationResponse[]>(responseContent, jsonOptions);

        if (result == null || result.Length == 0 || result[0].Translations.Length == 0)
        {
            throw new InvalidOperationException("Translation API returned empty result");
        }

        return result[0].Translations[0].Text;
    }

    /// <summary>
    /// Determines if an HTTP exception is eligible for fallback retry
    /// </summary>
    private static bool IsFallbackEligible(HttpRequestException ex)
    {
        var statusCode = ex.StatusCode;

        // Fallback on:
        // - 429 Too Many Requests (rate limit)
        // - 403 Forbidden (quota exceeded)
        // - 401 Unauthorized (invalid key)
        return statusCode is HttpStatusCode.TooManyRequests or
                             HttpStatusCode.Forbidden or
                             HttpStatusCode.Unauthorized;
    }

    /// <summary>
    /// Attempts to parse Azure error response JSON
    /// </summary>
    private (int? Code, string Message) TryParseErrorDetails(string errorContent)
    {
        try
        {
            var error = JsonSerializer.Deserialize<AzureErrorResponse>(errorContent);
            if (error?.Error != null)
            {
                return (error.Error.Code, error.Error.Message);
            }
        }
        catch (Exception ex)
        {
            // Parsing failed, return raw content
            _logger.LogDebug(ex, "Failed to parse Azure error response JSON. Using raw content instead.");
        }

        return (null, errorContent);
    }

    /// <summary>
    /// Calculates the next quota reset date (1st of next month)
    /// </summary>
    private static DateTime GetNextQuotaResetDate()
    {
        var now = DateTime.UtcNow;
        // Quota resets on 1st of next month
        return new DateTime(now.Year, now.Month, 1).AddMonths(1);
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

    private class AzureErrorResponse
    {
        public required ErrorDetail Error { get; set; }
    }

    private class ErrorDetail
    {
        public int Code { get; set; }
        public required string Message { get; set; }
    }
}

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
    /// Azure Translator fallback API key (optional secondary account)
    /// Used automatically when primary account fails with 401, 403, or 429 errors
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
