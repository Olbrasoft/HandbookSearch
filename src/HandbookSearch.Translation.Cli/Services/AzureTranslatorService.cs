using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olbrasoft.HandbookSearch.Translation.Cli.Configuration;

namespace Olbrasoft.HandbookSearch.Translation.Cli.Services;

/// <summary>
/// Azure Translator service implementation with rate limiting and fallback support
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

        // Configure HttpClient base address only (headers set per-request for fallback support)
        _httpClient.BaseAddress = new Uri(_options.Endpoint);
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
            "Translating {CharCount} characters to {TargetLang} using primary account, delay: {Delay}ms",
            text.Length,
            targetLanguage,
            delay.TotalMilliseconds);

        // Apply rate limiting delay BEFORE API call
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }

        // Try translation with fallback support
        return await TranslateWithFallbackAsync(text, targetLanguage, sourceLanguage, cancellationToken);
    }

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
                _options.ApiKey, "primary (olbrasoft@gmail.com)", cancellationToken);
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

            _logger.LogInformation("Retrying with fallback account (virtualniassistent@email.cz)");

            // Try fallback API key
            try
            {
                var result = await SendTranslationRequestAsync(
                    text, targetLanguage, sourceLanguage,
                    _options.FallbackApiKey, "fallback (virtualniassistent@email.cz)", cancellationToken);

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
        }
    }

    private async Task<string> SendTranslationRequestAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage,
        string apiKey,
        string accountDescription,
        CancellationToken cancellationToken)
    {
        // Build request URL
        var route = $"/translate?api-version=3.0&to={targetLanguage}";
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
            if (response.StatusCode == (HttpStatusCode)429 &&
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
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<TranslationResponse[]>(responseContent, options);

        if (result == null || result.Length == 0 || result[0].Translations.Length == 0)
        {
            throw new InvalidOperationException("Translation API returned empty result");
        }

        return result[0].Translations[0].Text;
    }

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

    private static (int? Code, string Message) TryParseErrorDetails(string errorContent)
    {
        try
        {
            var error = JsonSerializer.Deserialize<AzureErrorResponse>(errorContent);
            if (error?.Error != null)
            {
                return (error.Error.Code, error.Error.Message);
            }
        }
        catch
        {
            // Parsing failed, return raw content
        }

        return (null, errorContent);
    }

    private static DateTime GetNextQuotaResetDate()
    {
        var now = DateTime.UtcNow;
        // Quota resets on 1st of next month
        return new DateTime(now.Year, now.Month, 1).AddMonths(1);
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
