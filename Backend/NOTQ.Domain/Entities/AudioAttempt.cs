using NOTQ.Domain.Common;

namespace NOTQ.Domain.Entities;

public class AudioAttempt : BaseEntity<Guid>
{
    public Guid SessionId { get; set; }
    public int WordId { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public double? DurationSeconds { get; set; }

    public PracticeSession Session { get; set; } = null!;
    public PracticeWord Word { get; set; } = null!;
    public AnalysisResult? AnalysisResult { get; set; }
}
