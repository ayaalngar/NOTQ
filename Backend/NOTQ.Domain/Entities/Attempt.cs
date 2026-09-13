namespace NOTQ.Domain.Entities;

public class Attempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public int WordId { get; set; }
    public int AttemptNumber { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string? Prediction { get; set; }
    public double? Confidence { get; set; }
    public string? IssueType { get; set; }
    public string? DetectedWord { get; set; }
    public string? FeedbackType { get; set; }
    public string? FeedbackMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Session Session { get; set; } = null!;
    public Word Word { get; set; } = null!;
}
