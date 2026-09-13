using System.Text.Json.Serialization;

namespace NOTQ.Infrastructure.AI;

public class RailwayAnalyzeRequest
{
    [JsonPropertyName("expected_word")]
    public string ExpectedWord { get; set; } = string.Empty;

    [JsonPropertyName("predicted_word")]
    public string? PredictedWord { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }
}

public class RailwayAnalyzeResponse
{
    [JsonPropertyName("expected_word")]
    public string ExpectedWord { get; set; } = string.Empty;

    [JsonPropertyName("predicted_word")]
    public string? PredictedWord { get; set; }

    [JsonPropertyName("is_correct")]
    public bool IsCorrect { get; set; }

    [JsonPropertyName("needs_retry")]
    public bool NeedsRetry { get; set; }

    [JsonPropertyName("errors")]
    public List<RailwayErrorItem> Errors { get; set; } = new();

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class RailwayErrorItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("expected")]
    public string? Expected { get; set; }

    [JsonPropertyName("predicted")]
    public string? Predicted { get; set; }
}

public class RailwaySessionReportResponse
{
    [JsonPropertyName("screening_flag")]
    public bool ScreeningFlag { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("patterns_of_concern")]
    public List<RailwayPatternItem> PatternsOfConcern { get; set; } = new();

    [JsonPropertyName("disclaimer")]
    public string Disclaimer { get; set; } = string.Empty;
}

public class RailwayPatternItem
{
    [JsonPropertyName("target_sound_or_letter")]
    public string TargetSoundOrLetter { get; set; } = string.Empty;

    [JsonPropertyName("error_types")]
    public Dictionary<string, int> ErrorTypes { get; set; } = new();

    [JsonPropertyName("occurrences")]
    public int Occurrences { get; set; }
}
