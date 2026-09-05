using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.DTOs.Analysis;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Enums;

namespace NOTQ.Infrastructure.AI;

public class AiSpeechAnalysisService : ISpeechAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiSpeechAnalysisService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AiSpeechAnalysisService(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<AiSpeechAnalysisService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var aiOptions = options.Value;
        if (!string.IsNullOrWhiteSpace(aiOptions.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(aiOptions.BaseUrl);
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(aiOptions.TimeoutSeconds > 0 ? aiOptions.TimeoutSeconds : 15);
    }

    public async Task<SpeechAnalysisResult> AnalyzeAsync(
        Stream audio,
        string expectedWord,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AudioAnalysisStarted: External AI inference for word '{ExpectedWord}'", expectedWord);

        try
        {
            using var content = new MultipartFormDataContent();

            var audioContent = new StreamContent(audio);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            content.Add(audioContent, "audio", "pronunciation.wav");

            content.Add(new StringContent(expectedWord), "expectedWord");

            var response = await _httpClient.PostAsync("/predict", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("AudioAnalysisFailed: AI service returned HTTP status code {StatusCode}", response.StatusCode);
                throw new AiServiceUnavailableException($"Speech analysis service returned HTTP {response.StatusCode}.");
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var aiResponse = JsonSerializer.Deserialize<AiPredictionResponse>(responseString, _jsonOptions);

            if (aiResponse == null)
            {
                _logger.LogError("AudioAnalysisFailed: AI service returned an empty response");
                throw new AiServiceUnavailableException("Empty response received from speech analysis service.");
            }

            var prediction = ParsePrediction(aiResponse.Prediction);
            var issueType = ParseIssueType(aiResponse.IssueType);

            _logger.LogInformation(
                "AudioAnalysisCompleted: AI result prediction={Prediction}, confidence={Confidence}, issueType={IssueType}",
                prediction, aiResponse.Confidence, issueType);

            return new SpeechAnalysisResult
            {
                Prediction = prediction,
                Confidence = aiResponse.Confidence,
                IssueType = issueType,
                DetectedWord = aiResponse.DetectedWord
            };
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "AudioAnalysisFailed: AI service request timed out");
            throw new AiServiceUnavailableException("Speech analysis service timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AudioAnalysisFailed: Could not connect to AI service");
            throw new AiServiceUnavailableException("Speech analysis service is currently unreachable. Please try again later.");
        }
        catch (AiServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioAnalysisFailed: Unexpected error during AI analysis");
            throw new AiServiceUnavailableException("An error occurred during speech analysis. Please try again.");
        }
    }

    private static PronunciationPrediction ParsePrediction(string? prediction)
    {
        if (string.Equals(prediction, "Correct", StringComparison.OrdinalIgnoreCase))
        {
            return PronunciationPrediction.Correct;
        }

        return PronunciationPrediction.Incorrect;
    }

    private static IssueType ParseIssueType(string? issueType)
    {
        if (string.IsNullOrWhiteSpace(issueType))
        {
            return IssueType.None;
        }

        return issueType.ToLowerInvariant() switch
        {
            "substitution" => IssueType.Substitution,
            "omission" => IssueType.Omission,
            "distortion" => IssueType.Distortion,
            _ => IssueType.Unknown
        };
    }

    private class AiPredictionResponse
    {
        public string? Prediction { get; set; }
        public double Confidence { get; set; }
        public string? IssueType { get; set; }
        public string? DetectedWord { get; set; }
    }
}
