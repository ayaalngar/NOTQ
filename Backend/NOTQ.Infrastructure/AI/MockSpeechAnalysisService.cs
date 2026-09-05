using Microsoft.Extensions.Logging;
using NOTQ.Application.DTOs.Analysis;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Enums;

namespace NOTQ.Infrastructure.AI;

public class MockSpeechAnalysisService : ISpeechAnalysisService
{
    private readonly ILogger<MockSpeechAnalysisService> _logger;

    public MockSpeechAnalysisService(ILogger<MockSpeechAnalysisService> logger)
    {
        _logger = logger;
    }

    public async Task<SpeechAnalysisResult> AnalyzeAsync(
        Stream audio,
        string expectedWord,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AudioAnalysisStarted: Mock speech analysis for word '{ExpectedWord}'", expectedWord);

        // Simulate short processing latency (50ms)
        await Task.Delay(50, cancellationToken);

        var normalized = expectedWord.Trim();

        SpeechAnalysisResult result;

        switch (normalized)
        {
            case "سمكة":
                result = new SpeechAnalysisResult
                {
                    Prediction = PronunciationPrediction.Incorrect,
                    Confidence = 0.87,
                    IssueType = IssueType.Substitution,
                    DetectedWord = "تمكة"
                };
                break;

            case "سيارة":
                result = new SpeechAnalysisResult
                {
                    Prediction = PronunciationPrediction.Incorrect,
                    Confidence = 0.82,
                    IssueType = IssueType.Substitution,
                    DetectedWord = "تيارة"
                };
                break;

            case "شمس":
                result = new SpeechAnalysisResult
                {
                    Prediction = PronunciationPrediction.Incorrect,
                    Confidence = 0.85,
                    IssueType = IssueType.Substitution,
                    DetectedWord = "ثمس"
                };
                break;

            case "قطة":
                result = new SpeechAnalysisResult
                {
                    Prediction = PronunciationPrediction.Incorrect,
                    Confidence = 0.89,
                    IssueType = IssueType.Substitution,
                    DetectedWord = "تطة"
                };
                break;

            case "كتاب":
                result = new SpeechAnalysisResult
                {
                    Prediction = PronunciationPrediction.Correct,
                    Confidence = 0.94,
                    IssueType = IssueType.None,
                    DetectedWord = "كتاب"
                };
                break;

            case "بطة":
                result = new SpeechAnalysisResult
                {
                    Prediction = PronunciationPrediction.Correct,
                    Confidence = 0.96,
                    IssueType = IssueType.None,
                    DetectedWord = "بطة"
                };
                break;

            default:
                // Default fallback: 80% correct simulation
                result = new SpeechAnalysisResult
                {
                    Prediction = PronunciationPrediction.Correct,
                    Confidence = 0.91,
                    IssueType = IssueType.None,
                    DetectedWord = normalized
                };
                break;
        }

        _logger.LogInformation(
            "AudioAnalysisCompleted: Prediction={Prediction}, Confidence={Confidence}, IssueType={IssueType}, DetectedWord={DetectedWord}",
            result.Prediction, result.Confidence, result.IssueType, result.DetectedWord);

        return result;
    }
}
