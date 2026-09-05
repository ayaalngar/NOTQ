using NOTQ.Domain.Enums;

namespace NOTQ.Application.DTOs.Progress;

public class ChildProgressDto
{
    public int SessionsCompleted { get; set; }
    public int WordsPracticed { get; set; }
    public double AverageScore { get; set; }
    public double LatestScore { get; set; }
    public SessionTrend Trend { get; set; }
}
