using Microsoft.Extensions.Logging;
using NOTQ.Application.DTOs.Screening;
using NOTQ.Application.Interfaces;

namespace NOTQ.Infrastructure.AI;

public class RailwaySessionScreeningService : ISessionScreeningService
{
    private readonly PronunciationAnalysisApiClient _apiClient;
    private readonly ILogger<RailwaySessionScreeningService> _logger;

    public RailwaySessionScreeningService(
        PronunciationAnalysisApiClient apiClient,
        ILogger<RailwaySessionScreeningService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<SessionScreeningResult> GenerateScreeningReportAsync(
        IReadOnlyList<WordAttemptAnalysisSummary> attempts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var railwayAttempts = attempts.Select(a => new RailwayAnalyzeResponse
            {
                ExpectedWord = a.ExpectedWord,
                PredictedWord = a.PredictedWord,
                IsCorrect = a.IsCorrect,
                NeedsRetry = a.NeedsRetry,
                Errors = a.Errors.Select(e => new RailwayErrorItem
                {
                    Type = e.Type,
                    Expected = e.Expected,
                    Predicted = e.Predicted
                }).ToList(),
                Reason = a.Reason
            }).ToList();

            var response = await _apiClient.GenerateSessionReportAsync(railwayAttempts, cancellationToken);

            if (response == null)
            {
                _logger.LogWarning("Session screening API returned an empty response");
                return new SessionScreeningResult
                {
                    IsServiceUnavailable = true,
                    Summary = "Empty response received from session screening service."
                };
            }

            var patterns = response.PatternsOfConcern.Select(p => new ConcerningPattern
            {
                TargetSoundOrLetter = p.TargetSoundOrLetter,
                ErrorTypes = p.ErrorTypes,
                Occurrences = p.Occurrences
            }).ToList();

            return new SessionScreeningResult
            {
                RequiresProfessionalReview = response.ScreeningFlag,
                Summary = response.Message,
                ConcerningPatterns = patterns,
                Disclaimer = response.Disclaimer,
                IsServiceUnavailable = false
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Session screening service unreachable or timed out");
            return new SessionScreeningResult
            {
                IsServiceUnavailable = true,
                Summary = "Session screening service is currently unreachable."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during session screening");
            return new SessionScreeningResult
            {
                IsServiceUnavailable = true,
                Summary = "An unexpected error occurred during session screening."
            };
        }
    }
}
