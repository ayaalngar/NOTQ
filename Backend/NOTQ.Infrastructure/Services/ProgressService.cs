using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Progress;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Enums;

namespace NOTQ.Infrastructure.Services;

public class ProgressService : IProgressService
{
    private readonly IApplicationDbContext _context;
    private readonly IScoringService _scoringService;

    public ProgressService(
        IApplicationDbContext context,
        IScoringService scoringService)
    {
        _context = context;
        _scoringService = scoringService;
    }

    public async Task<ChildProgressDto> GetChildProgressAsync(
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
            throw new ForbiddenException("You do not have access to this child's progress.");
        }

        var completedSessions = await _context.PracticeSessions
            .AsNoTracking()
            .Where(s => s.ChildId == childId && s.Status == SessionStatus.Completed)
            .OrderBy(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        if (completedSessions.Count == 0)
        {
            return new ChildProgressDto
            {
                SessionsCompleted = 0,
                WordsPracticed = 0,
                AverageScore = 0.0,
                LatestScore = 0.0,
                Trend = SessionTrend.InsufficientData
            };
        }

        var scores = completedSessions.Select(s => s.Score).ToList();
        var totalWords = completedSessions.Sum(s => s.TotalAttempts);
        var averageScore = Math.Round(scores.Average(), 2);
        var latestScore = scores[^1];
        var trend = _scoringService.DetermineTrend(scores);

        return new ChildProgressDto
        {
            SessionsCompleted = completedSessions.Count,
            WordsPracticed = totalWords,
            AverageScore = averageScore,
            LatestScore = latestScore,
            Trend = trend
        };
    }
}
