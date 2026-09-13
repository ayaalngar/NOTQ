using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using NOTQ.Application.DTOs.Analysis;
using NOTQ.Application.DTOs.Screening;
using NOTQ.Infrastructure.AI;
using Xunit;

namespace NOTQ.Tests;

public class PronunciationAnalysisTests
{
    private static IHttpClientFactory CreateMockHttpClientFactory(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://diagnose-api-production-c9ac.up.railway.app/")
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(PronunciationAnalysisApiClient.ClientName))
            .Returns(httpClient);

        return factoryMock.Object;
    }

    private static IHttpClientFactory CreateFailingHttpClientFactory()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://diagnose-api-production-c9ac.up.railway.app/")
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient(PronunciationAnalysisApiClient.ClientName))
            .Returns(httpClient);

        return factoryMock.Object;
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsCorrectResult_WhenPronunciationIsCorrect()
    {
        // Arrange
        var railwayResponse = new RailwayAnalyzeResponse
        {
            ExpectedWord = "سمكة",
            PredictedWord = "سمكة",
            IsCorrect = true,
            NeedsRetry = false,
            Errors = new List<RailwayErrorItem>(),
            Reason = null
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(railwayResponse))
        };

        var factory = CreateMockHttpClientFactory(httpResponse);
        var apiClient = new PronunciationAnalysisApiClient(factory, NullLogger<PronunciationAnalysisApiClient>.Instance);
        var service = new RailwayPronunciationAnalysisService(apiClient, NullLogger<RailwayPronunciationAnalysisService>.Instance);

        // Act
        var result = await service.AnalyzeAsync("سمكة", "سمكة");

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeFalse();
        result.IsCorrect.Should().BeTrue();
        result.NeedsRetry.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsErrors_WhenSubstitutionOccurs()
    {
        // Arrange
        var railwayResponse = new RailwayAnalyzeResponse
        {
            ExpectedWord = "سمكة",
            PredictedWord = "تمكة",
            IsCorrect = false,
            NeedsRetry = false,
            Errors = new List<RailwayErrorItem>
            {
                new() { Type = "substitution", Expected = "س", Predicted = "ت" }
            },
            Reason = null
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(railwayResponse))
        };

        var factory = CreateMockHttpClientFactory(httpResponse);
        var apiClient = new PronunciationAnalysisApiClient(factory, NullLogger<PronunciationAnalysisApiClient>.Instance);
        var service = new RailwayPronunciationAnalysisService(apiClient, NullLogger<RailwayPronunciationAnalysisService>.Instance);

        // Act
        var result = await service.AnalyzeAsync("سمكة", "تمكة", confidence: null);

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeFalse();
        result.IsCorrect.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Type.Should().Be("substitution");
        result.Errors[0].Expected.Should().Be("س");
        result.Errors[0].Predicted.Should().Be("ت");
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsServiceUnavailable_WhenApiIsUnreachable()
    {
        // Arrange
        var factory = CreateFailingHttpClientFactory();
        var apiClient = new PronunciationAnalysisApiClient(factory, NullLogger<PronunciationAnalysisApiClient>.Instance);
        var service = new RailwayPronunciationAnalysisService(apiClient, NullLogger<RailwayPronunciationAnalysisService>.Instance);

        // Act
        var result = await service.AnalyzeAsync("سمكة", "تمكة");

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeTrue();
        result.Reason.Should().Contain("unreachable");
    }

    [Fact]
    public async Task GenerateScreeningReportAsync_ReturnsReport_WhenApiSucceeds()
    {
        // Arrange
        var railwayReport = new RailwaySessionReportResponse
        {
            ScreeningFlag = true,
            Message = "تم ملاحظة نمط نطق متكرر يحتاج إلى تدريب في الحروف التالية: /س/.",
            PatternsOfConcern = new List<RailwayPatternItem>
            {
                new()
                {
                    TargetSoundOrLetter = "س",
                    ErrorTypes = new Dictionary<string, int> { ["substitution"] = 3 },
                    Occurrences = 3
                }
            },
            Disclaimer = "هذا التقييم هو فحص آلي مبدئي."
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(railwayReport))
        };

        var factory = CreateMockHttpClientFactory(httpResponse);
        var apiClient = new PronunciationAnalysisApiClient(factory, NullLogger<PronunciationAnalysisApiClient>.Instance);
        var service = new RailwaySessionScreeningService(apiClient, NullLogger<RailwaySessionScreeningService>.Instance);

        var attempts = new List<WordAttemptAnalysisSummary>
        {
            new()
            {
                ExpectedWord = "سمكة",
                PredictedWord = "تمكة",
                IsCorrect = false,
                Errors = new List<PronunciationErrorDetail>
                {
                    new() { Type = "substitution", Expected = "س", Predicted = "ت" }
                }
            }
        };

        // Act
        var result = await service.GenerateScreeningReportAsync(attempts);

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeFalse();
        result.RequiresProfessionalReview.Should().BeTrue();
        result.Summary.Should().Contain("/س/");
        result.ConcerningPatterns.Should().HaveCount(1);
        result.ConcerningPatterns[0].TargetSoundOrLetter.Should().Be("س");
        result.ConcerningPatterns[0].Occurrences.Should().Be(3);
        result.ConcerningPatterns[0].ErrorTypes.Should().ContainKey("substitution");
        result.Disclaimer.Should().Be("هذا التقييم هو فحص آلي مبدئي.");
    }

    [Fact]
    public async Task GenerateScreeningReportAsync_ReturnsServiceUnavailable_WhenApiThrows()
    {
        // Arrange
        var factory = CreateFailingHttpClientFactory();
        var apiClient = new PronunciationAnalysisApiClient(factory, NullLogger<PronunciationAnalysisApiClient>.Instance);
        var service = new RailwaySessionScreeningService(apiClient, NullLogger<RailwaySessionScreeningService>.Instance);

        // Act
        var result = await service.GenerateScreeningReportAsync(new List<WordAttemptAnalysisSummary>());

        // Assert
        result.Should().NotBeNull();
        result.IsServiceUnavailable.Should().BeTrue();
        result.Summary.Should().Contain("unreachable");
    }
}
