using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NOTQ.Domain.Entities;
using NOTQ.Domain.Enums;
using NOTQ.Infrastructure.Persistence;
using NOTQ.Infrastructure.Services;
using Xunit;

namespace NOTQ.Tests;

public class PatternDetectionTests
{
    [Fact]
    public async Task DetectPatterns_ShouldDetectRepeatedSoundPatternAndUseScreeningLanguage()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var childId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var word1 = new PracticeWord { Id = 1, Word = "سمكة", TargetSound = "س" };
        var word2 = new PracticeWord { Id = 2, Word = "سيارة", TargetSound = "س" };
        context.PracticeWords.AddRange(word1, word2);

        var session = new PracticeSession
        {
            Id = sessionId,
            ChildId = childId,
            Status = SessionStatus.Completed
        };
        context.PracticeSessions.Add(session);

        // Add 2 repeated substitution attempts on target sound "س"
        var attempt1 = new AudioAttempt
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            WordId = 1,
            AudioUrl = "/audio/1.wav",
            Word = word1,
            Session = session,
            AnalysisResult = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                Prediction = PronunciationPrediction.Incorrect,
                IssueType = IssueType.Substitution,
                Confidence = 0.88,
                DetectedWord = "تمكة"
            }
        };

        var attempt2 = new AudioAttempt
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            WordId = 2,
            AudioUrl = "/audio/2.wav",
            Word = word2,
            Session = session,
            AnalysisResult = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                Prediction = PronunciationPrediction.Incorrect,
                IssueType = IssueType.Substitution,
                Confidence = 0.84,
                DetectedWord = "تيارة"
            }
        };

        context.AudioAttempts.AddRange(attempt1, attempt2);
        await context.SaveChangesAsync();

        var patternService = new PatternDetectionService(context);

        var patterns = await patternService.DetectPatternsAsync(childId);

        patterns.Should().HaveCount(1);
        var pattern = patterns[0];
        pattern.TargetSound.Should().Be("س");
        pattern.Occurrences.Should().Be(2);
        pattern.Confidence.Should().Be(0.86);

        // Crucial test: Ensure strictly screening language without medical diagnostic claims
        pattern.Observation.Should().Contain("Repeated pronunciation pattern detected");
        pattern.Observation.Should().NotContain("speech disorder");
        pattern.Observation.Should().NotContain("diagnosed");
        pattern.Observation.Should().NotContain("pathology");
    }
}
