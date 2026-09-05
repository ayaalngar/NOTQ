using NOTQ.Domain.Common;
using NOTQ.Domain.Enums;

namespace NOTQ.Domain.Entities;

public class AnalysisResult : BaseEntity<Guid>
{
    public Guid AttemptId { get; set; }
    public PronunciationPrediction Prediction { get; set; }
    public double Confidence { get; set; }
    public IssueType IssueType { get; set; } = IssueType.None;
    public string? DetectedWord { get; set; }

    public AudioAttempt Attempt { get; set; } = null!;
}
