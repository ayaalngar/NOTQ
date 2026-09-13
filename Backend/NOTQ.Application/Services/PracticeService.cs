using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Entities;

namespace NOTQ.Application.Services;

public class PracticeService : IPracticeService
{
    private readonly IApplicationDbContext _context;

    public PracticeService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StartSessionResponseDto> StartPracticeSessionAsync(
        Guid childId,
        string? categoryFilter = null,
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
            Type = "Practice",
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

    public async Task<CompletePracticeResponseDto> CompletePracticeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.Attempts)
                .ThenInclude(a => a.Word)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("Session", sessionId);
        }

        var latestAttemptsPerWord = session.Attempts
            .GroupBy(a => a.WordId)
            .Select(g => g.OrderByDescending(a => a.AttemptNumber).First())
            .ToList();

        var wordStatuses = latestAttemptsPerWord.Select(a =>
        {
            var isWordCorrect = string.Equals(a.Prediction, "correct", StringComparison.OrdinalIgnoreCase);
            return new PracticeWordStatusDto
            {
                Word = a.Word?.WordText ?? string.Empty,
                Status = isWordCorrect ? "correct" : "needsPractice"
            };
        }).ToList();

        var correctCount = wordStatuses.Count(w => w.Status == "correct");
        var needsPracticeCount = wordStatuses.Count(w => w.Status == "needsPractice");

        session.Status = "Completed";
        session.CompletedAt = DateTime.UtcNow;
        session.TotalWords = wordStatuses.Count;
        session.CorrectCount = correctCount;
        session.NeedsPracticeCount = needsPracticeCount;
        session.Score = wordStatuses.Count > 0 ? Math.Round((double)correctCount / wordStatuses.Count * 100, 2) : 0.0;

        await _context.SaveChangesAsync(cancellationToken);

        return new CompletePracticeResponseDto
        {
            CorrectCount = correctCount,
            NeedsPracticeCount = needsPracticeCount,
            Words = wordStatuses
        };
    }
}
