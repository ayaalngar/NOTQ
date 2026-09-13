namespace NOTQ.Infrastructure.AI;

public class RailwayAnalysisOptions
{
    public const string SectionName = "PronunciationAnalysisApi";
    public string BaseUrl { get; set; } = "https://diagnose-api-production-c9ac.up.railway.app";
    public int TimeoutSeconds { get; set; } = 15;
}
