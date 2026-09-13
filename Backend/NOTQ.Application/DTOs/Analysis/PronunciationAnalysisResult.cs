namespace NOTQ.Application.DTOs.Analysis;

public class PronunciationAnalysisResult
{
    public bool IsCorrect { get; set; }
    public bool NeedsRetry { get; set; }
    public IReadOnlyList<PronunciationErrorDetail> Errors { get; set; } = Array.Empty<PronunciationErrorDetail>();
    public string? Reason { get; set; }
    public bool IsServiceUnavailable { get; set; }
}

public class PronunciationErrorDetail
{
    public string Type { get; set; } = string.Empty;
    public string? Expected { get; set; }
    public string? Predicted { get; set; }
}
