using NOTQ.Application.DTOs.Analysis;
using NOTQ.Application.DTOs.Auth;
using NOTQ.Domain.Enums;

namespace NOTQ.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
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
