using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;

namespace NOTQ.Application.Services;

public class HomeService : IHomeService
{
    private readonly IApplicationDbContext _context;

    public HomeService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HomeScreenResponseDto> GetHomeScreenAsync(
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

        var totalWords = await _context.Words.CountAsync(cancellationToken);

        var completedWords = await _context.Attempts
            .Where(a => a.Session.ChildId == childId && a.Prediction == "correct")
            .Select(a => a.WordId)
            .Distinct()
            .CountAsync(cancellationToken);

        var wordsAttempted = await _context.Attempts
            .Where(a => a.Session.ChildId == childId && a.Prediction != null)
            .GroupBy(a => a.WordId)
            .Select(g => g.OrderByDescending(a => a.CreatedAt).Select(a => a.Prediction).FirstOrDefault())
            .ToListAsync(cancellationToken);

        var practiceWords = wordsAttempted.Count(p => !string.Equals(p, "correct", StringComparison.OrdinalIgnoreCase));

        var hasCompletedAssessment = await _context.Sessions
            .AnyAsync(s => s.ChildId == childId && s.Type == "Assessment" && s.Status == "Completed", cancellationToken);

        var journey = hasCompletedAssessment
            ? new HomeJourneyDto
            {
                SuggestedAction = "continuePractice",
                Title = "تابع التمارين اليومية",
                Description = "استمر في التدريب لتحسين نطق الحروف والكلمات!"
            }
            : new HomeJourneyDto
            {
                SuggestedAction = "startAssessment",
                Title = "ابدأ جلسة التقييم",
                Description = "دعنا نكتشف مهارات النطق لديك من خلال رحلة ممتعة!"
            };

        return new HomeScreenResponseDto
        {
            Child = new HomeChildDto
            {
                Id = child.Id,
                Name = child.Name,
                Age = child.Age,
                Avatar = child.AvatarId
            },
            Progress = new HomeProgressDto
            {
                CompletedWords = completedWords,
                TotalWords = totalWords,
                PracticeWords = practiceWords
            },
            Journey = journey
        };
    }
}
