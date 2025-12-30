using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Olbrasoft.HandbookSearch.Translation.Cli.Configuration;
using Olbrasoft.HandbookSearch.Translation.Cli.Services;
using Olbrasoft.HandbookSearch.Translation.Tests.Helpers;

namespace Olbrasoft.HandbookSearch.Translation.Tests.Services;

/// <summary>
/// Tests for Azure Translator fallback mechanism (Issue #49)
/// Verifies automatic failover from primary to fallback account on errors
/// </summary>
public class AzureTranslatorFallbackTests
{
    private const string PrimaryApiKey = "primary-key-12345";
    private const string FallbackApiKey = "fallback-key-67890";
    private const string Region = "westeurope";
    private const string TestText = "Hello world";
    private const string TranslatedText = "Ahoj světe";

    [Fact]
    public async Task TranslateAsync_Primary429_FallbackSucceeds()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslatorService>>();
        var options = CreateOptions(withFallback: true);

        // First call (primary) returns 429, second call (fallback) succeeds
        var handlerMock = new Mock<HttpMessageHandler>();
        var callCount = 0;
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // Primary account - 429 rate limit
                    var response = new HttpResponseMessage
                    {
                        StatusCode = (System.Net.HttpStatusCode)429,
                        Content = new StringContent(@"{
                            ""error"": {
                                ""code"": 429000,
                                ""message"": ""Too many requests""
                            }
                        }")
                    };
                    response.Headers.Add("Retry-After", "60");
                    return response;
                }
                // Fallback account - success
                return new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent($@"[{{
                        ""translations"": [{{
                            ""text"": ""{TranslatedText}"",
                            ""to"": ""cs""
                        }}]
                    }}]")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new AzureTranslatorService(httpClient, options, mockLogger.Object);

        // Act
        var result = await service.TranslateAsync(TestText, "cs");

        // Assert
        Assert.Equal(TranslatedText, result);

        // Verify fallback was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("fallback account")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TranslateAsync_Primary403_FallbackSucceeds()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslatorService>>();
        var options = CreateOptions(withFallback: true);

        // First call (primary) returns 403, second call (fallback) succeeds
        var handlerMock = new Mock<HttpMessageHandler>();
        var callCount = 0;
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // Primary account - 403 quota exceeded
                    return new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.Forbidden,
                        Content = new StringContent(@"{
                            ""error"": {
                                ""code"": 403000,
                                ""message"": ""Out of call volume quota. Quota will be replenished in 05:12:34.""
                            }
                        }")
                    };
                }
                // Fallback account - success
                return new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent($@"[{{
                        ""translations"": [{{
                            ""text"": ""{TranslatedText}"",
                            ""to"": ""cs""
                        }}]
                    }}]")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new AzureTranslatorService(httpClient, options, mockLogger.Object);

        // Act
        var result = await service.TranslateAsync(TestText, "cs");

        // Assert
        Assert.Equal(TranslatedText, result);

        // Verify fallback was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("fallback account")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TranslateAsync_Primary401_FallbackSucceeds()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslatorService>>();
        var options = CreateOptions(withFallback: true);

        // First call (primary) returns 401, second call (fallback) succeeds
        var handlerMock = new Mock<HttpMessageHandler>();
        var callCount = 0;
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // Primary account - 401 unauthorized
                    return new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.Unauthorized,
                        Content = new StringContent(@"{
                            ""error"": {
                                ""code"": 401001,
                                ""message"": ""The request is not authorized because credentials are missing or invalid.""
                            }
                        }")
                    };
                }
                // Fallback account - success
                return new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent($@"[{{
                        ""translations"": [{{
                            ""text"": ""{TranslatedText}"",
                            ""to"": ""cs""
                        }}]
                    }}]")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new AzureTranslatorService(httpClient, options, mockLogger.Object);

        // Act
        var result = await service.TranslateAsync(TestText, "cs");

        // Assert
        Assert.Equal(TranslatedText, result);
    }

    [Fact]
    public async Task TranslateAsync_BothAccounts429_ThrowsWithHelpfulMessage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslatorService>>();
        var options = CreateOptions(withFallback: true);

        // Both calls return 429
        var handlerMock = HttpMessageHandlerMockFactory.Create429RateLimit("60");
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com")
        };
        var service = new AzureTranslatorService(httpClient, options, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.TranslateAsync(TestText, "cs"));

        Assert.Contains("Both accounts failed", exception.Message);
        Assert.Contains("TooManyRequests", exception.Message);

        // Verify critical error was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Both accounts failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_BothAccounts403_ThrowsWithQuotaResetDate()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslatorService>>();
        var options = CreateOptions(withFallback: true);

        // Both calls return 403
        var handlerMock = HttpMessageHandlerMockFactory.Create403QuotaExceeded("05:12:34");
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com")
        };
        var service = new AzureTranslatorService(httpClient, options, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.TranslateAsync(TestText, "cs"));

        Assert.Contains("Both accounts failed", exception.Message);
        Assert.Contains("Quota resets on", exception.Message);

        // Verify quota reset date is included
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        Assert.Contains(nextMonth.Year.ToString(), exception.Message);
    }

    [Fact]
    public async Task TranslateAsync_NoFallbackKey_ThrowsImmediately()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslatorService>>();
        var options = CreateOptions(withFallback: false); // No fallback key

        var handlerMock = HttpMessageHandlerMockFactory.Create429RateLimit("60");
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com")
        };
        var service = new AzureTranslatorService(httpClient, options, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.TranslateAsync(TestText, "cs"));

        Assert.DoesNotContain("fallback", exception.Message);

        // Verify error was logged about missing fallback
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("no fallback key configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_429WithRetryAfterHeader_LogsRetryAfter()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslatorService>>();
        var options = CreateOptions(withFallback: false);

        var handlerMock = HttpMessageHandlerMockFactory.Create429RateLimit("120"); // 2 minutes
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com")
        };
        var service = new AzureTranslatorService(httpClient, options, mockLogger.Object);

        // Act
        try
        {
            await service.TranslateAsync(TestText, "cs");
        }
        catch (HttpRequestException)
        {
            // Expected
        }

        // Assert - Verify Retry-After was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry after") && v.ToString()!.Contains("120")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_Success_ReturnsTranslatedText()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslatorService>>();
        var options = CreateOptions(withFallback: false);

        var handlerMock = HttpMessageHandlerMockFactory.CreateSuccess(TranslatedText);
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com")
        };
        var service = new AzureTranslatorService(httpClient, options, mockLogger.Object);

        // Act
        var result = await service.TranslateAsync(TestText, "cs");

        // Assert
        Assert.Equal(TranslatedText, result);

        // Verify translation started log
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Translating")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static IOptions<AzureTranslatorOptions> CreateOptions(bool withFallback)
    {
        var options = new AzureTranslatorOptions
        {
            ApiKey = PrimaryApiKey,
            Region = Region,
            Endpoint = "https://api.cognitive.microsofttranslator.com",
            FallbackApiKey = withFallback ? FallbackApiKey : null
        };

        return Options.Create(options);
    }
}
