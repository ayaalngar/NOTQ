using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Reports;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Enums;

namespace NOTQ.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDbContext _context;
    private readonly IScoringService _scoringService;
    private readonly IPatternDetectionService _patternService;

    public ReportService(
        IApplicationDbContext context,
        IScoringService scoringService,
        IPatternDetectionService patternService)
    {
        _context = context;
        _scoringService = scoringService;
        _patternService = patternService;
    }

    public async Task<ChildReportDto> GetChildReportAsync(
        Guid parentId,
        Guid childId,
        CancellationToken cancellationToken = default)
    {
        var child = await _context.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId, cancellationToken);

        if (child == null)
        {
            throw new NotFoundException("Child", childId);
        }

        if (child.ParentId != parentId)
        {
            throw new ForbiddenException("You do not have access to this child's reports.");
        }

        var completedSessions = await _context.PracticeSessions
            .AsNoTracking()
            .Where(s => s.ChildId == childId && s.Status == SessionStatus.Completed)
            .ToListAsync(cancellationToken);

        var patterns = await _patternService.DetectPatternsAsync(childId, cancellationToken);

        var scores = completedSessions.Select(s => s.Score).ToList();
        var totalWords = completedSessions.Sum(s => s.TotalAttempts);
        var consistencyScore = _scoringService.CalculateConsistencyScore(scores);
        var averageScore = scores.Count > 0 ? scores.Average() : 0.0;

        var (recommendation, notes) = DetermineRecommendation(completedSessions.Count, averageScore, patterns);

        return new ChildReportDto
        {
            ChildId = child.Id,
            ChildName = child.Name,
            Sessions = completedSessions.Count,
            WordsPracticed = totalWords,
            ConsistencyScore = consistencyScore,
            Patterns = patterns,
            Recommendation = recommendation,
            RecommendationNotes = notes,
            Disclaimer = "NOTQ is a screening and early-awareness tool, not a medical diagnostic system. Observations are intended to guide parents and educators. Professional evaluation may be recommended."
        };
    }

    public async Task<SessionReportDto> GetSessionReportAsync(
        Guid parentId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.PracticeSessions
            .Include(s => s.Child)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("PracticeSession", sessionId);
        }

        if (session.Child.ParentId != parentId)
        {
            throw new ForbiddenException("You do not have access to this session's report.");
        }

        var patterns = await _patternService.DetectSessionPatternsAsync(sessionId, cancellationToken);

        return new SessionReportDto
        {
            SessionId = session.Id,
            ChildId = session.ChildId,
            ChildName = session.Child.Name,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            TotalWords = session.TotalAttempts,
            CorrectAttempts = session.CorrectAttempts,
            IncorrectAttempts = Math.Max(0, session.TotalAttempts - session.CorrectAttempts),
            Score = session.Score,
            Patterns = patterns,
            Disclaimer = "NOTQ is a screening and early-awareness tool, not a medical diagnostic system."
        };
    }

    private static (RecommendationLevel Level, string Notes) DetermineRecommendation(
        int sessionCount,
        double averageScore,
        List<PatternDto> patterns)
    {
        if (sessionCount == 0)
        {
            return (RecommendationLevel.StandardPractice, "No completed practice sessions yet. Start regular practice sessions to monitor progress.");
        }

        if (patterns.Count >= 2 || (sessionCount >= 3 && averageScore < 0.55))
        {
            return (
                RecommendationLevel.ProfessionalEvaluation,
                "Repeated pronunciation patterns were observed across multiple sessions. A consultation or formal screening review with a certified speech-language specialist may be beneficial."
            );
        }

        if (patterns.Count == 1 || averageScore < 0.75)
        {
            return (
                RecommendationLevel.MonitoredPractice,
                "Some pronunciation inconsistencies were observed. Recommend continuing regular guided practice sessions to observe consistency."
            );
        }

        return (
            RecommendationLevel.StandardPractice,
            "Pronunciation performance is strong and consistent. Continue regular practice sessions."
        );
    }
}
