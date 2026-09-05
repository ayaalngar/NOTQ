namespace NOTQ.Infrastructure.AI;

public class AiOptions
{
    public const string SectionName = "AiService";

    public bool UseMock { get; set; } = true;
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 10;
}
