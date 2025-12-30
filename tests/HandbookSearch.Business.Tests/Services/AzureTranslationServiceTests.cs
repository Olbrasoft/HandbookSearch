using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Olbrasoft.HandbookSearch.Business.Services;

namespace HandbookSearch.Business.Tests.Services;

/// <summary>
/// Tests for Azure Translator fallback mechanism integrated into Business layer (Issue #33)
/// Verifies automatic failover from primary to fallback account on errors
/// </summary>
public class AzureTranslationServiceTests
{
    private const string PrimaryApiKey = "primary-key-12345";
    private const string FallbackApiKey = "fallback-key-67890";
    private const string Region = "westeurope";
    private const string TestMarkdown = "Hello world";
    private const string TranslatedMarkdown = "Ahoj světe";

    [Fact]
    public async Task TranslateToCzechAsync_Primary429_FallbackSucceeds()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslationService>>();
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
                            ""text"": ""{TranslatedMarkdown}"",
                            ""to"": ""cs""
                        }}]
                    }}]")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new AzureTranslationService(httpClient, options, mockLogger.Object);

        // Act
        var result = await service.TranslateToCzechAsync(TestMarkdown);

        // Assert
        Assert.Equal(TranslatedMarkdown, result);

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
    public async Task TranslateToCzechAsync_Primary403_FallbackSucceeds()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslationService>>();
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
                            ""text"": ""{TranslatedMarkdown}"",
                            ""to"": ""cs""
                        }}]
                    }}]")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new AzureTranslationService(httpClient, options, mockLogger.Object);

        // Act
        var result = await service.TranslateToCzechAsync(TestMarkdown);

        // Assert
        Assert.Equal(TranslatedMarkdown, result);

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
    public async Task TranslateToCzechAsync_Primary401_FallbackSucceeds()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslationService>>();
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
                            ""text"": ""{TranslatedMarkdown}"",
                            ""to"": ""cs""
                        }}]
                    }}]")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new AzureTranslationService(httpClient, options, mockLogger.Object);

        // Act
        var result = await service.TranslateToCzechAsync(TestMarkdown);

        // Assert
        Assert.Equal(TranslatedMarkdown, result);
    }

    [Fact]
    public async Task TranslateToCzechAsync_BothAccounts429_ThrowsWithHelpfulMessage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslationService>>();
        var options = CreateOptions(withFallback: true);

        // Both calls return 429
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
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
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com")
        };
        var service = new AzureTranslationService(httpClient, options, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.TranslateToCzechAsync(TestMarkdown));

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
    public async Task TranslateToCzechAsync_BothAccounts403_ThrowsWithQuotaResetDate()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslationService>>();
        var options = CreateOptions(withFallback: true);

        // Both calls return 403
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
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
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com")
        };
        var service = new AzureTranslationService(httpClient, options, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.TranslateToCzechAsync(TestMarkdown));

        Assert.Contains("Both accounts failed", exception.Message);
        Assert.Contains("Quota resets on", exception.Message);

        // Verify quota reset date is included (handle year boundary edge case)
        var currentYear = DateTime.UtcNow.Year.ToString();
        var nextYear = DateTime.UtcNow.AddYears(1).Year.ToString();
        Assert.True(
            exception.Message.Contains(currentYear) || exception.Message.Contains(nextYear),
            $"Expected quota reset year to be {currentYear} or {nextYear}");
    }

    [Fact]
    public async Task TranslateToCzechAsync_NoFallbackKey_ThrowsImmediately()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslationService>>();
        var options = CreateOptions(withFallback: false); // No fallback key

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
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
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com")
        };
        var service = new AzureTranslationService(httpClient, options, mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.TranslateToCzechAsync(TestMarkdown));

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
    public async Task TranslateToCzechAsync_Success_ReturnsTranslatedMarkdown()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureTranslationService>>();
        var options = CreateOptions(withFallback: false);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                return new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent($@"[{{
                        ""translations"": [{{
                            ""text"": ""{TranslatedMarkdown}"",
                            ""to"": ""cs""
                        }}]
                    }}]")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.cognitive.microsofttranslator.com")
        };
        var service = new AzureTranslationService(httpClient, options, mockLogger.Object);

        // Act
        var result = await service.TranslateToCzechAsync(TestMarkdown);

        // Assert
        Assert.Equal(TranslatedMarkdown, result);

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
