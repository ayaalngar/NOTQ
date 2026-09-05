using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Reports;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Enums;

namespace NOTQ.Infrastructure.Services;

public class PatternDetectionService : IPatternDetectionService
{
    private readonly IApplicationDbContext _context;
    private const int PatternOccurrenceThreshold = 2;

    public PatternDetectionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PatternDto>> DetectPatternsAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        var attempts = await _context.AudioAttempts
            .Include(a => a.AnalysisResult)
            .Include(a => a.Word)
            .Include(a => a.Session)
            .Where(a => a.Session.ChildId == childId &&
                        a.AnalysisResult != null &&
                        a.AnalysisResult.Prediction == PronunciationPrediction.Incorrect)
            .ToListAsync(cancellationToken);

        return AggregatePatterns(attempts);
    }

    public async Task<List<PatternDto>> DetectSessionPatternsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var attempts = await _context.AudioAttempts
            .Include(a => a.AnalysisResult)
            .Include(a => a.Word)
            .Where(a => a.SessionId == sessionId &&
                        a.AnalysisResult != null &&
                        a.AnalysisResult.Prediction == PronunciationPrediction.Incorrect)
            .ToListAsync(cancellationToken);

        return AggregatePatterns(attempts);
    }

    private static List<PatternDto> AggregatePatterns(List<Domain.Entities.AudioAttempt> incorrectAttempts)
    {
        var patterns = new List<PatternDto>();

        // Group by TargetSound (e.g. "س", "ش")
        var soundGroups = incorrectAttempts
            .Where(a => !string.IsNullOrEmpty(a.Word.TargetSound))
            .GroupBy(a => a.Word.TargetSound!);

        foreach (var soundGroup in soundGroups)
        {
            var count = soundGroup.Count();
            if (count >= PatternOccurrenceThreshold)
            {
                var avgConfidence = Math.Round(soundGroup.Average(a => a.AnalysisResult!.Confidence), 2);
                var dominantIssue = soundGroup
                    .GroupBy(a => a.AnalysisResult!.IssueType)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                patterns.Add(new PatternDto
                {
                    Type = dominantIssue.ToString(),
                    TargetSound = soundGroup.Key,
                    Occurrences = count,
                    Confidence = avgConfidence,
                    Observation = $"Repeated pronunciation pattern detected on target sound /{soundGroup.Key}/ ({dominantIssue} observed {count} times with {avgConfidence * 100:0}% avg confidence)."
                });
            }
        }

        // Group by IssueType if no specific sound group met threshold
        if (patterns.Count == 0)
        {
            var issueGroups = incorrectAttempts
                .Where(a => a.AnalysisResult!.IssueType != IssueType.None)
                .GroupBy(a => a.AnalysisResult!.IssueType);

            foreach (var issueGroup in issueGroups)
            {
                var count = issueGroup.Count();
                if (count >= PatternOccurrenceThreshold)
                {
                    var avgConfidence = Math.Round(issueGroup.Average(a => a.AnalysisResult!.Confidence), 2);
                    patterns.Add(new PatternDto
                    {
                        Type = issueGroup.Key.ToString(),
                        TargetSound = null,
                        Occurrences = count,
                        Confidence = avgConfidence,
                        Observation = $"Repeated {issueGroup.Key} pattern detected across {count} attempts with {avgConfidence * 100:0}% avg confidence."
                    });
                }
            }
        }

        return patterns;
    }
}
