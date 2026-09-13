using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NOTQ.Infrastructure.AI;

public class PronunciationAnalysisApiClient
{
    public const string ClientName = "NotqPronunciationAnalysisApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PronunciationAnalysisApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PronunciationAnalysisApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<PronunciationAnalysisApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<RailwayAnalyzeResponse?> AnalyzePronunciationAsync(
        RailwayAnalyzeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calling Railway /api/v1/analyze-pronunciation for expectedWord: '{ExpectedWord}', predictedWord: '{PredictedWord}'",
            request.ExpectedWord, request.PredictedWord);

        var client = _httpClientFactory.CreateClient(ClientName);
        var response = await client.PostAsJsonAsync("api/v1/analyze-pronunciation", request, _jsonOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RailwayAnalyzeResponse>(_jsonOptions, cancellationToken);
        return result;
    }

    public async Task<RailwaySessionReportResponse?> GenerateSessionReportAsync(
        IReadOnlyList<RailwayAnalyzeResponse> attempts,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calling Railway /api/v1/session-report with {Count} attempt items",
            attempts.Count);

        var client = _httpClientFactory.CreateClient(ClientName);
        var response = await client.PostAsJsonAsync("api/v1/session-report", attempts, _jsonOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RailwaySessionReportResponse>(_jsonOptions, cancellationToken);
        return result;
    }
}
