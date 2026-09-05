using NOTQ.Domain.Enums;

namespace NOTQ.Application.DTOs.Reports;

public class PatternDto
{
    public string Type { get; set; } = string.Empty;
    public string? TargetSound { get; set; }
    public int Occurrences { get; set; }
    public double Confidence { get; set; }
    public string Observation { get; set; } = string.Empty;
}

public class ChildReportDto
{
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public int WordsPracticed { get; set; }
    public double ConsistencyScore { get; set; }
    public List<PatternDto> Patterns { get; set; } = new();
    public RecommendationLevel Recommendation { get; set; }
    public string RecommendationNotes { get; set; } = string.Empty;
    public string Disclaimer { get; set; } = "NOTQ is a screening and early-awareness tool, not a medical diagnostic system. Observations are intended to guide parents and educators. Professional evaluation may be recommended.";
}

public class SessionReportDto
{
    public Guid SessionId { get; set; }
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalWords { get; set; }
    public int CorrectAttempts { get; set; }
    public int IncorrectAttempts { get; set; }
    public double Score { get; set; }
    public List<PatternDto> Patterns { get; set; } = new();
    public string Disclaimer { get; set; } = "NOTQ is a screening and early-awareness tool, not a medical diagnostic system.";
}
