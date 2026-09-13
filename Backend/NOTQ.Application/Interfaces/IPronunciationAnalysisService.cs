using NOTQ.Application.DTOs.Analysis;

namespace NOTQ.Application.Interfaces;

public interface IPronunciationAnalysisService
{
    Task<PronunciationAnalysisResult> AnalyzeAsync(
        string expectedWord,
        string? predictedWord,
        double? confidence = null,
        CancellationToken cancellationToken = default);
}
