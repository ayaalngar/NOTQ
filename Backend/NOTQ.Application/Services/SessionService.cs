using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Sessions;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Entities;
using NOTQ.Domain.Enums;

namespace NOTQ.Application.Services;

public class SessionService : ISessionService
{
    private readonly IApplicationDbContext _context;
    private readonly IScoringService _scoringService;

    public SessionService(
        IApplicationDbContext context,
        IScoringService scoringService)
    {
        _context = context;
        _scoringService = scoringService;
    }

    public async Task<SessionResponseDto> StartSessionAsync(Guid parentId, StartSessionDto dto, CancellationToken cancellationToken = default)
    {
        var child = await _context.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == dto.ChildId, cancellationToken);

        if (child == null)
        {
            throw new NotFoundException("Child", dto.ChildId);
        }

        if (child.ParentId != parentId)
        {
            throw new ForbiddenException("You do not have access to start sessions for this child.");
        }

        var session = new PracticeSession
        {
            Id = Guid.NewGuid(),
            ChildId = dto.ChildId,
            StartedAt = DateTime.UtcNow,
            Status = SessionStatus.InProgress,
            TotalAttempts = 0,
            CorrectAttempts = 0,
            Score = 0.0,
            CreatedAt = DateTime.UtcNow
        };

        _context.PracticeSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return new SessionResponseDto
        {
            SessionId = session.Id,
            ChildId = session.ChildId,
            ChildName = child.Name,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            Status = session.Status,
            TotalAttempts = session.TotalAttempts,
            CorrectAttempts = session.CorrectAttempts,
            Score = session.Score
        };
    }

    public async Task<SessionResponseDto> GetSessionByIdAsync(Guid parentId, Guid sessionId, CancellationToken cancellationToken = default)
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
            throw new ForbiddenException("You do not have access to this session.");
        }

        return new SessionResponseDto
        {
            SessionId = session.Id,
            ChildId = session.ChildId,
            ChildName = session.Child.Name,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            Status = session.Status,
            TotalAttempts = session.TotalAttempts,
            CorrectAttempts = session.CorrectAttempts,
            Score = session.Score
        };
    }

    public async Task<CompleteSessionResponseDto> CompleteSessionAsync(Guid parentId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.PracticeSessions
            .Include(s => s.Child)
            .Include(s => s.AudioAttempts)
                .ThenInclude(a => a.AnalysisResult)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("PracticeSession", sessionId);
        }

        if (session.Child.ParentId != parentId)
        {
            throw new ForbiddenException("You do not have permission to complete this session.");
        }

        if (session.Status == SessionStatus.Completed)
        {
            var correctCount = session.AudioAttempts.Count(a => a.AnalysisResult?.Prediction == PronunciationPrediction.Correct);
            var totalCount = session.AudioAttempts.Count;
            return new CompleteSessionResponseDto
            {
                SessionId = session.Id,
                Status = session.Status,
                TotalWords = totalCount,
                CorrectAttempts = correctCount,
                IncorrectAttempts = Math.Max(0, totalCount - correctCount),
                Score = session.Score
            };
        }

        var total = session.AudioAttempts.Count;
        var correct = session.AudioAttempts.Count(a => a.AnalysisResult?.Prediction == PronunciationPrediction.Correct);
        var score = _scoringService.CalculateSessionScore(correct, total);

        session.Status = SessionStatus.Completed;
        session.CompletedAt = DateTime.UtcNow;
        session.TotalAttempts = total;
        session.CorrectAttempts = correct;
        session.Score = score;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new CompleteSessionResponseDto
        {
            SessionId = session.Id,
            Status = session.Status,
            TotalWords = total,
            CorrectAttempts = correct,
            IncorrectAttempts = Math.Max(0, total - correct),
            Score = score
        };
    }

    public async Task<IEnumerable<SessionResponseDto>> GetSessionsByChildAsync(Guid parentId, Guid childId, CancellationToken cancellationToken = default)
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
            throw new ForbiddenException("You do not have access to this child's sessions.");
        }

        var sessions = await _context.PracticeSessions
            .AsNoTracking()
            .Where(s => s.ChildId == childId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);

        return sessions.Select(s => new SessionResponseDto
        {
            SessionId = s.Id,
            ChildId = s.ChildId,
            ChildName = child.Name,
            StartedAt = s.StartedAt,
            CompletedAt = s.CompletedAt,
            Status = s.Status,
            TotalAttempts = s.TotalAttempts,
            CorrectAttempts = s.CorrectAttempts,
            Score = s.Score
        });
    }
}
