using NOTQ.Domain.Enums;

namespace NOTQ.Application.DTOs.Analysis;

public class SpeechAnalysisResult
{
    public PronunciationPrediction Prediction { get; set; }
    public double Confidence { get; set; }
    public IssueType IssueType { get; set; } = IssueType.None;
    public string? DetectedWord { get; set; }
}
