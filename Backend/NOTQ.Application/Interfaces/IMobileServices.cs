using NOTQ.Application.DTOs.Mobile;

namespace NOTQ.Application.Interfaces;

public interface IMobileChildService
{
    Task<CreateChildMobileResponseDto> CreateChildAsync(
        CreateChildMobileRequestDto dto,
        CancellationToken cancellationToken = default);
}

public interface IAssessmentService
{
    Task<IReadOnlyList<AssessmentWordDto>> GetAssessmentWordsAsync(
        int count = 5,
        CancellationToken cancellationToken = default);

    Task<StartSessionResponseDto> StartAssessmentSessionAsync(
        Guid childId,
        CancellationToken cancellationToken = default);

    Task<CompleteAssessmentResponseDto> CompleteAssessmentSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

public interface IPracticeService
{
    Task<StartSessionResponseDto> StartPracticeSessionAsync(
        Guid childId,
        string? categoryFilter = null,
        CancellationToken cancellationToken = default);

    Task<CompletePracticeResponseDto> CompletePracticeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

public interface IAttemptProcessingService
{
    Task<SubmitAttemptResponseDto> ProcessAssessmentAttemptAsync(
        Guid sessionId,
        int wordId,
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<PracticeAttemptResponseDto> ProcessPracticeAttemptAsync(
        Guid sessionId,
        int wordId,
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default);
}

public interface IHomeService
{
    Task<HomeScreenResponseDto> GetHomeScreenAsync(
        Guid childId,
        CancellationToken cancellationToken = default);
}
