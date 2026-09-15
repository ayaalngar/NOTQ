using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using NOTQ.Infrastructure.AI;
using Xunit;

namespace NOTQ.Tests;

public class GradioWordVerificationServiceTests
{
    private static (IHttpClientFactory Factory, List<HttpRequestMessage> CapturedRequests) CreateMockHttpClientFactory(
        Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
    {
        var capturedRequests = new List<HttpRequestMessage>();
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                capturedRequests.Add(req);
                return handlerFunc(req);
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://bas77sel-notq-api.hf.space/")
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(GradioWordVerificationService.ClientName))
            .Returns(httpClient);

        return (factoryMock.Object, capturedRequests);
    }

    [Fact]
    public async Task VerifyWordAsync_WhenSuccessful_CallsV2EndpointWithAudioPath_AndReturnsTranscribedText()
    {
        // Arrange
        const string targetWord = "كلب";
        const string transcribedWord = "كلب";
        const string remotePath = "/tmp/gradio/abc/attempt.wav";
        const string eventId = "test-event-123";

        var (factory, capturedRequests) = CreateMockHttpClientFactory(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;

            if (uri.EndsWith("gradio_api/upload"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new[] { remotePath }))
                };
            }

            if (uri.EndsWith("gradio_api/call/v2/transcribe"))
            {
                // Verify request body contains audio_path instead of data
                var body = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(body!);
                doc.RootElement.TryGetProperty("audio_path", out var audioPathProp).Should().BeTrue();
                audioPathProp.GetProperty("path").GetString().Should().Be(remotePath);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { event_id = eventId }))
                };
            }

            if (uri.EndsWith($"gradio_api/call/transcribe/{eventId}"))
            {
                var sseContent = $"event: complete\ndata: [\"{transcribedWord}\"]\n\n";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = new GradioWordVerificationService(factory, NullLogger<GradioWordVerificationService>.Instance);
        using var audioStream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await service.VerifyWordAsync(audioStream, targetWord);

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeFalse();
        result.TranscribedText.Should().Be(transcribedWord);
        result.Matched.Should().BeTrue();
        result.MatchScore.Should().Be(1.0);
        result.ErrorMessage.Should().BeNull();

        capturedRequests.Should().HaveCount(3);
        capturedRequests[1].RequestUri?.ToString().Should().Contain("gradio_api/call/v2/transcribe");
    }

    [Fact]
    public async Task VerifyWordAsync_WhenZeroGpuQuotaExceeded_ReturnsServiceUnavailableWithQuotaErrorMessage()
    {
        // Arrange
        const string targetWord = "كلب";
        const string remotePath = "/tmp/gradio/abc/attempt.wav";
        const string eventId = "quota-exceeded-event";

        var (factory, _) = CreateMockHttpClientFactory(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;

            if (uri.EndsWith("gradio_api/upload"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new[] { remotePath }))
                };
            }

            if (uri.EndsWith("gradio_api/call/v2/transcribe"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { event_id = eventId }))
                };
            }

            if (uri.EndsWith($"gradio_api/call/transcribe/{eventId}"))
            {
                var sseContent = "event: error\ndata: ZeroGPU quota exceeded: Authenticate with a Hugging Face token for more quota\n\n";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = new GradioWordVerificationService(factory, NullLogger<GradioWordVerificationService>.Instance);
        using var audioStream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await service.VerifyWordAsync(audioStream, targetWord);

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeTrue();
        result.TranscribedText.Should().BeEmpty();
        result.Matched.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ZeroGPU quota exceeded");
    }

    [Fact]
    public async Task VerifyWordAsync_WhenParameterDataErrorReturned_ReturnsServiceUnavailableWithInvalidGradioRequest()
    {
        // Arrange
        const string targetWord = "كلب";
        const string remotePath = "/tmp/gradio/abc/attempt.wav";
        const string eventId = "param-data-error-event";

        var (factory, _) = CreateMockHttpClientFactory(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;

            if (uri.EndsWith("gradio_api/upload"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new[] { remotePath }))
                };
            }

            if (uri.EndsWith("gradio_api/call/v2/transcribe"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { event_id = eventId }))
                };
            }

            if (uri.EndsWith($"gradio_api/call/transcribe/{eventId}"))
            {
                var sseContent = "event: error\ndata: Parameter `data` is not a valid key-word argument\n\n";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = new GradioWordVerificationService(factory, NullLogger<GradioWordVerificationService>.Instance);
        using var audioStream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await service.VerifyWordAsync(audioStream, targetWord);

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeTrue();
        result.TranscribedText.Should().BeEmpty();
        result.Matched.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid Gradio request");
    }

    [Fact]
    public async Task VerifyWordAsync_WhenUploadFails_ReturnsServiceUnavailableWithUploadError()
    {
        // Arrange
        var (factory, _) = CreateMockHttpClientFactory(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("Upstream gateway error")
            };
        });

        var service = new GradioWordVerificationService(factory, NullLogger<GradioWordVerificationService>.Instance);
        using var audioStream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await service.VerifyWordAsync(audioStream, "كلب");

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeTrue();
        result.ErrorMessage.Should().Contain("502");
    }

    [Fact]
    public async Task VerifyWordAsync_WhenCallFails_ReturnsServiceUnavailableWithCallError()
    {
        // Arrange
        var (factory, _) = CreateMockHttpClientFactory(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;

            if (uri.EndsWith("gradio_api/upload"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new[] { "/tmp/audio.wav" }))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server crashed")
            };
        });

        var service = new GradioWordVerificationService(factory, NullLogger<GradioWordVerificationService>.Instance);
        using var audioStream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await service.VerifyWordAsync(audioStream, "كلب");

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeTrue();
        result.ErrorMessage.Should().Contain("500");
    }

    [Theory]
    [InlineData("كَلْبٌ", "كلب", 1.0, true)]
    [InlineData("أحمد", "احمد", 1.0, true)]
    [InlineData("شجرة", "شجره", 1.0, true)]
    [InlineData("مستشفى", "مستشفي", 1.0, true)]
    [InlineData("كلب", "كلبه", 0.85, true)]
    [InlineData("كلب", "قلب", 0.66, false)]
    [InlineData("سمكة", "قرد", 0.0, false)]
    public void CalculateMatchScore_NormalizesArabicAndComputesExpectedScore(
        string target, string transcribed, double minExpectedScore, bool shouldMatch)
    {
        var score = GradioWordVerificationService.CalculateMatchScore(target, transcribed);

        score.Should().BeGreaterThanOrEqualTo(minExpectedScore);
        var isMatched = score >= 0.70;
        isMatched.Should().Be(shouldMatch);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task VerifyWordAsync_LiveSpaceIntegration_ExecutesAgainstHuggingFaceSpace()
    {
        var samplePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "audio_sample.wav"));
        if (!File.Exists(samplePath))
        {
            return;
        }

        var factoryMock = new Mock<IHttpClientFactory>();
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://bas77sel-notq-api.hf.space/"),
            Timeout = TimeSpan.FromSeconds(45)
        };
        factoryMock.Setup(f => f.CreateClient(GradioWordVerificationService.ClientName)).Returns(httpClient);

        var service = new GradioWordVerificationService(factoryMock.Object, NullLogger<GradioWordVerificationService>.Instance);
        using var stream = File.OpenRead(samplePath);

        var result = await service.VerifyWordAsync(stream, "هيل");

        // The live call should either succeed (transcribing Arabic text) or report clean failure (e.g. ZeroGPU quota)
        if (!result.IsServiceUnavailable)
        {
            result.TranscribedText.Should().Be("هيل");
            result.Matched.Should().BeTrue();
            result.MatchScore.Should().Be(1.0);
        }
        else
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }
}
