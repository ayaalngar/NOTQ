using Microsoft.Extensions.Logging;
using NOTQ.Application.DTOs.Analysis;
using NOTQ.Application.Interfaces;

namespace NOTQ.Infrastructure.AI;

public class RailwayPronunciationAnalysisService : IPronunciationAnalysisService
{
    private readonly PronunciationAnalysisApiClient _apiClient;
    private readonly ILogger<RailwayPronunciationAnalysisService> _logger;

    public RailwayPronunciationAnalysisService(
        PronunciationAnalysisApiClient apiClient,
        ILogger<RailwayPronunciationAnalysisService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PronunciationAnalysisResult> AnalyzeAsync(
        string expectedWord,
        string? predictedWord,
        double? confidence = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new RailwayAnalyzeRequest
            {
                ExpectedWord = expectedWord,
                PredictedWord = predictedWord,
                Confidence = confidence
            };

            var response = await _apiClient.AnalyzePronunciationAsync(request, cancellationToken);

            if (response == null)
            {
                _logger.LogWarning("Pronunciation analysis API returned an empty response for word '{ExpectedWord}'", expectedWord);
                return new PronunciationAnalysisResult
                {
                    IsServiceUnavailable = true,
                    Reason = "Empty response received from pronunciation analysis service."
                };
            }

            var errors = response.Errors.Select(e => new PronunciationErrorDetail
            {
                Type = e.Type,
                Expected = e.Expected,
                Predicted = e.Predicted
            }).ToList();

            return new PronunciationAnalysisResult
            {
                IsCorrect = response.IsCorrect,
                NeedsRetry = response.NeedsRetry,
                Errors = errors,
                Reason = response.Reason,
                IsServiceUnavailable = false
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Pronunciation analysis service unreachable or timed out for word '{ExpectedWord}'", expectedWord);
            return new PronunciationAnalysisResult
            {
                IsServiceUnavailable = true,
                Reason = "Pronunciation analysis service is currently unreachable."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during pronunciation analysis for word '{ExpectedWord}'", expectedWord);
            return new PronunciationAnalysisResult
            {
                IsServiceUnavailable = true,
                Reason = "An unexpected error occurred during pronunciation analysis."
            };
        }
    }
}
