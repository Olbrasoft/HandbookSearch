using System.Net;
using Moq;
using Moq.Protected;

namespace Olbrasoft.HandbookSearch.Translation.Tests.Helpers;

/// <summary>
/// Factory for creating mocked HttpMessageHandler instances for testing
/// </summary>
public static class HttpMessageHandlerMockFactory
{
    public static Mock<HttpMessageHandler> CreateSuccess(string translatedText)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateSuccessResponse(translatedText));
        return handler;
    }

    public static Mock<HttpMessageHandler> Create429RateLimit(string? retryAfter = "60")
    {
        var handler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = (HttpStatusCode)429,
            Content = new StringContent(@"{
                ""error"": {
                    ""code"": 429000,
                    ""message"": ""Too many requests""
                }
            }")
        };

        if (!string.IsNullOrEmpty(retryAfter))
        {
            response.Headers.Add("Retry-After", retryAfter);
        }

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return handler;
    }

    public static Mock<HttpMessageHandler> Create403QuotaExceeded(string? resetTime = "05:12:34")
    {
        var handler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Forbidden,
            Content = new StringContent($@"{{
                ""error"": {{
                    ""code"": 403000,
                    ""message"": ""Out of call volume quota. Quota will be replenished in {resetTime}.""
                }}
            }}")
        };

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return handler;
    }

    public static Mock<HttpMessageHandler> Create401Unauthorized()
    {
        var handler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized,
            Content = new StringContent(@"{
                ""error"": {
                    ""code"": 401001,
                    ""message"": ""The request is not authorized because credentials are missing or invalid.""
                }
            }")
        };

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return handler;
    }

    private static HttpResponseMessage CreateSuccessResponse(string translatedText)
    {
        return new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent($@"[{{
                ""translations"": [{{
                    ""text"": ""{translatedText}"",
                    ""to"": ""cs""
                }}]
            }}]")
        };
    }
}
