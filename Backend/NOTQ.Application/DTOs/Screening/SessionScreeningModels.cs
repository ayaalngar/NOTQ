using NOTQ.Application.DTOs.Analysis;

namespace NOTQ.Application.DTOs.Screening;

public class WordAttemptAnalysisSummary
{
    public string ExpectedWord { get; set; } = string.Empty;
    public string? PredictedWord { get; set; }
    public bool IsCorrect { get; set; }
    public bool NeedsRetry { get; set; }
    public IReadOnlyList<PronunciationErrorDetail> Errors { get; set; } = Array.Empty<PronunciationErrorDetail>();
    public string? Reason { get; set; }
}

public class SessionScreeningResult
{
    public bool RequiresProfessionalReview { get; set; }
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<ConcerningPattern> ConcerningPatterns { get; set; } = Array.Empty<ConcerningPattern>();
    public string Disclaimer { get; set; } = string.Empty;
    public bool IsServiceUnavailable { get; set; }
}

public class ConcerningPattern
{
    public string TargetSoundOrLetter { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, int> ErrorTypes { get; set; } = new Dictionary<string, int>();
    public int Occurrences { get; set; }
}
