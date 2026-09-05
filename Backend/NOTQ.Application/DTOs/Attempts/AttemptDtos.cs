using Microsoft.AspNetCore.Http;
using NOTQ.Domain.Enums;

namespace NOTQ.Application.DTOs.Attempts;

public class SubmitAttemptRequestDto
{
    public IFormFile Audio { get; set; } = null!;
    public int WordId { get; set; }
}

public class FeedbackDto
{
    public string Type { get; set; } = string.Empty; // "Success", "Retry", "Encouragement"
    public string Message { get; set; } = string.Empty;
}

public class AttemptResponseDto
{
    public Guid AttemptId { get; set; }
    public int WordId { get; set; }
    public string Word { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public PronunciationPrediction Prediction { get; set; }
    public double Confidence { get; set; }
    public IssueType IssueType { get; set; }
    public string? DetectedWord { get; set; }
    public FeedbackDto Feedback { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
