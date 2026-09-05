using NOTQ.Application.DTOs.Analysis;
using NOTQ.Application.DTOs.Attempts;
using NOTQ.Application.DTOs.Auth;
using NOTQ.Application.DTOs.Children;
using NOTQ.Application.DTOs.Progress;
using NOTQ.Application.DTOs.Reports;
using NOTQ.Application.DTOs.Sessions;
using NOTQ.Application.DTOs.Words;
using NOTQ.Domain.Enums;

namespace NOTQ.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IChildService
{
    Task<ChildResponseDto> CreateChildAsync(Guid parentId, CreateChildDto dto, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChildResponseDto>> GetChildrenByParentAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<ChildResponseDto> GetChildByIdAsync(Guid parentId, Guid childId, CancellationToken cancellationToken = default);
    Task<ChildResponseDto> UpdateChildAsync(Guid parentId, Guid childId, UpdateChildDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteChildAsync(Guid parentId, Guid childId, CancellationToken cancellationToken = default);
}

public interface ISessionService
{
    Task<SessionResponseDto> StartSessionAsync(Guid parentId, StartSessionDto dto, CancellationToken cancellationToken = default);
    Task<SessionResponseDto> GetSessionByIdAsync(Guid parentId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<CompleteSessionResponseDto> CompleteSessionAsync(Guid parentId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionResponseDto>> GetSessionsByChildAsync(Guid parentId, Guid childId, CancellationToken cancellationToken = default);
}

public interface IAttemptService
{
    Task<AttemptResponseDto> RecordAttemptAsync(Guid parentId, Guid sessionId, SubmitAttemptRequestDto dto, CancellationToken cancellationToken = default);
}

public interface IAudioStorageService
{
    Task<string> SaveAudioAsync(Stream audioStream, string originalFileName, CancellationToken cancellationToken = default);
    Task<bool> DeleteAudioAsync(string relativePath, CancellationToken cancellationToken = default);
}

public interface ISpeechAnalysisService
{
    Task<SpeechAnalysisResult> AnalyzeAsync(Stream audio, string expectedWord, CancellationToken cancellationToken = default);
}

public interface IScoringService
{
    double CalculateSessionScore(int correctAttempts, int totalAttempts);
    double CalculateConsistencyScore(IEnumerable<double> sessionScores);
    SessionTrend DetermineTrend(IReadOnlyList<double> chronologicalScores);
}

public interface IPatternDetectionService
{
    Task<List<PatternDto>> DetectPatternsAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<List<PatternDto>> DetectSessionPatternsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public interface IProgressService
{
    Task<ChildProgressDto> GetChildProgressAsync(Guid parentId, Guid childId, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<ChildReportDto> GetChildReportAsync(Guid parentId, Guid childId, CancellationToken cancellationToken = default);
    Task<SessionReportDto> GetSessionReportAsync(Guid parentId, Guid sessionId, CancellationToken cancellationToken = default);
}

public interface IPracticeWordService
{
    Task<IEnumerable<PracticeWordDto>> GetAllWordsAsync(string? difficulty = null, string? targetSound = null, CancellationToken cancellationToken = default);
    Task<PracticeWordDto> GetWordByIdAsync(int id, CancellationToken cancellationToken = default);
}
