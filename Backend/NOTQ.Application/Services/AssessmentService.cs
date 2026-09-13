using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Entities;

namespace NOTQ.Application.Services;

public class AssessmentService : IAssessmentService
{
    private readonly IApplicationDbContext _context;

    public AssessmentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AssessmentWordDto>> GetAssessmentWordsAsync(
        int count = 5,
        CancellationToken cancellationToken = default)
    {
        var effectiveCount = count > 0 ? count : 5;

        var words = await _context.Words
            .AsNoTracking()
            .Where(w => w.Type == "Word")
            .OrderBy(w => w.Id)
            .Take(effectiveCount)
            .Select(w => new AssessmentWordDto
            {
                Id = w.Id,
                Word = w.WordText,
                ImageUrl = w.ImageUrl
            })
            .ToListAsync(cancellationToken);

        return words;
    }

    public async Task<StartSessionResponseDto> StartAssessmentSessionAsync(
        Guid childId,
        CancellationToken cancellationToken = default)
    {
        var childExists = await _context.Children
            .AsNoTracking()
            .AnyAsync(c => c.Id == childId, cancellationToken);

        if (!childExists)
        {
            throw new NotFoundException("Child", childId);
        }

        var session = new Session
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Type = "Assessment",
            Status = "InProgress",
            StartedAt = DateTime.UtcNow
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return new StartSessionResponseDto
        {
            SessionId = session.Id
        };
    }

    public async Task<CompleteAssessmentResponseDto> CompleteAssessmentSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.Attempts)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("Session", sessionId);
        }

        var latestAttemptsPerWord = session.Attempts
            .GroupBy(a => a.WordId)
            .Select(g => g.OrderByDescending(a => a.AttemptNumber).First())
            .ToList();

        var totalWords = latestAttemptsPerWord.Count;
        var correctAttempts = latestAttemptsPerWord.Count(a => string.Equals(a.Prediction, "correct", StringComparison.OrdinalIgnoreCase));
        var incorrectAttempts = totalWords - correctAttempts;
        var score = totalWords > 0 ? Math.Round((double)correctAttempts / totalWords * 100, 2) : 0.0;

        session.Status = "Completed";
        session.CompletedAt = DateTime.UtcNow;
        session.TotalWords = totalWords;
        session.CorrectCount = correctAttempts;
        session.NeedsPracticeCount = incorrectAttempts;
        session.Score = score;

        await _context.SaveChangesAsync(cancellationToken);

        return new CompleteAssessmentResponseDto
        {
            SessionId = session.Id,
            Status = "completed",
            TotalWords = totalWords,
            CorrectAttempts = correctAttempts,
            IncorrectAttempts = incorrectAttempts,
            Score = score
        };
    }
}
