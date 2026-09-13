using NOTQ.Application.DTOs.Screening;

namespace NOTQ.Application.Interfaces;

public interface ISessionScreeningService
{
    Task<SessionScreeningResult> GenerateScreeningReportAsync(
        IReadOnlyList<WordAttemptAnalysisSummary> attempts,
        CancellationToken cancellationToken = default);
}
