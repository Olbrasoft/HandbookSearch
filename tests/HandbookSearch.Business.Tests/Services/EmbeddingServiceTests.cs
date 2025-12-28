using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Olbrasoft.HandbookSearch.Business;
using Olbrasoft.HandbookSearch.Business.Services;

namespace HandbookSearch.Business.Tests.Services;

public class EmbeddingServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IOptions<OllamaOptions>> _mockOptions;
    private readonly EmbeddingService _service;

    public EmbeddingServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

        _mockOptions = new Mock<IOptions<OllamaOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(new OllamaOptions
        {
            BaseUrl = "http://localhost:11434",
            Model = "nomic-embed-text",
            Dimensions = 768
        });

        _service = new EmbeddingService(_httpClient, _mockOptions.Object);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ValidText_ReturnsEmbedding()
    {
        // Arrange
        var testText = "Test text for embedding";
        var expectedEmbedding = new float[768];
        for (int i = 0; i < 768; i++)
        {
            expectedEmbedding[i] = i * 0.001f;
        }

        var response = new
        {
            embedding = expectedEmbedding
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response)
            });

        // Act
        var result = await _service.GenerateEmbeddingAsync(testText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(768, result.Length);
        Assert.Equal(expectedEmbedding, result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_Null_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GenerateEmbeddingAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateEmbeddingAsync_EmptyOrWhiteSpace_ThrowsArgumentException(string text)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateEmbeddingAsync(text));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_HttpError_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Ollama error")
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.GenerateEmbeddingAsync("test"));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_InvalidResponseFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateEmbeddingAsync("test"));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WrongDimensions_ThrowsInvalidOperationException()
    {
        // Arrange
        var wrongSizeEmbedding = new float[512]; // Wrong size
        var response = new
        {
            embedding = wrongSizeEmbedding
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response)
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateEmbeddingAsync("test"));

        Assert.Contains("Expected 768 dimensions", exception.Message);
        Assert.Contains("got 512", exception.Message);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_SendsCorrectRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var expectedEmbedding = new float[768];

        var response = new
        {
            embedding = expectedEmbedding
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response)
            });

        // Act
        await _service.GenerateEmbeddingAsync("test text");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("http://localhost:11434/api/embeddings", capturedRequest.RequestUri?.ToString());
    }
}
