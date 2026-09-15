using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.DTOs.Analysis;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;
using NOTQ.Application.Services;
using NOTQ.Domain.Entities;
using NOTQ.Infrastructure.Persistence;

namespace NOTQ.Tests;

public class MobileEndpointTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CreateChild_WhenValid_CreatesStandaloneChildWithoutUserOrToken()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new MobileChildService(context);

        var request = new CreateChildMobileRequestDto
        {
            Name = "سارة",
            Age = 6,
            Avatar = "avatar_girl"
        };

        // Act
        var result = await service.CreateChildAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ChildId.Should().NotBeEmpty();
        result.Name.Should().Be("سارة");
        result.Age.Should().Be(6);
        result.Avatar.Should().Be("avatar_girl");

        var childInDb = await context.Children.FirstOrDefaultAsync(c => c.Id == result.ChildId);
        childInDb.Should().NotBeNull();
        childInDb!.Name.Should().Be("سارة");
        childInDb.Age.Should().Be(6);
        childInDb.AvatarId.Should().Be("avatar_girl");
    }

    [Fact]
    public async Task StartAssessmentSession_WhenChildDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var nonExistentChildId = Guid.NewGuid();
        var service = new AssessmentService(context);

        // Act & Assert
        var act = async () => await service.StartAssessmentSessionAsync(nonExistentChildId);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Child*not found*");
    }

    [Fact]
    public async Task StartPracticeSession_WhenChildDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var nonExistentChildId = Guid.NewGuid();
        var service = new PracticeService(context);

        // Act & Assert
        var act = async () => await service.StartPracticeSessionAsync(nonExistentChildId, "س");
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Child*not found*");
    }

    [Fact]
    public async Task ProcessAssessmentAttempt_WhenSessionDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var nonExistentSessionId = Guid.NewGuid();

        var audioStorageMock = new Mock<IAudioStorageService>();
        var wordVerificationMock = new Mock<IWordVerificationService>();
        var pronunciationAnalysisMock = new Mock<IPronunciationAnalysisService>();
        var loggerMock = new Mock<ILogger<AttemptProcessingService>>();

        var service = new AttemptProcessingService(
            context,
            audioStorageMock.Object,
            wordVerificationMock.Object,
            pronunciationAnalysisMock.Object,
            loggerMock.Object);

        using var fakeAudioStream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act & Assert
        var act = async () => await service.ProcessAssessmentAttemptAsync(
            nonExistentSessionId, 1, fakeAudioStream, "test.wav");

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Session*not found*");
    }

    [Fact]
    public async Task ProcessAssessmentAttempt_WhenCorrect_ReturnsStrictlyLowercaseCorrect_AndNullConfidence()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var child = new Child
        {
            Id = Guid.NewGuid(),
            Name = "يوسف",
            Age = 6,
            AvatarId = "avatar_boy"
        };
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Child = child,
            ChildId = child.Id,
            Type = "Assessment",
            Status = "InProgress",
            TotalWords = 5
        };
        var word = new Word
        {
            Id = 100,
            WordText = "كلب",
            Type = "Word",
            ImageUrl = "/assets/words/dog.png"
        };
        context.Children.Add(child);
        context.Sessions.Add(session);
        context.Words.Add(word);
        await context.SaveChangesAsync();

        var audioStorageMock = new Mock<IAudioStorageService>();
        audioStorageMock.Setup(a => a.SaveAudioAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/audio/test.wav");

        var wordVerificationMock = new Mock<IWordVerificationService>();
        wordVerificationMock.Setup(w => w.VerifyWordAsync(It.IsAny<Stream>(), "كلب", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WordVerificationResult { Matched = true, TranscribedText = "كلب", MatchScore = 1.0 });

        double? capturedConfidence = 1.0; // sentinel to verify it was set to null
        var pronunciationAnalysisMock = new Mock<IPronunciationAnalysisService>();
        pronunciationAnalysisMock.Setup(p => p.AnalyzeAsync("كلب", "كلب", It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string?, double?, CancellationToken>((e, p, conf, ct) => capturedConfidence = conf)
            .ReturnsAsync(new PronunciationAnalysisResult { IsCorrect = true });

        var loggerMock = new Mock<ILogger<AttemptProcessingService>>();
        var service = new AttemptProcessingService(
            context,
            audioStorageMock.Object,
            wordVerificationMock.Object,
            pronunciationAnalysisMock.Object,
            loggerMock.Object);

        using var fakeAudioStream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = await service.ProcessAssessmentAttemptAsync(
            session.Id, word.Id, fakeAudioStream, "test.wav");

        // Assert
        result.Should().NotBeNull();
        result.Prediction.Should().Be("correct");
        result.Confidence.Should().BeNull();
        result.Feedback.Should().NotBeNull();
        result.Feedback.Type.Should().Be("correct", "feedback.type MUST be exact lowercase string 'correct'");
        capturedConfidence.Should().BeNull("confidence parameter passed to AnalyzeAsync MUST be null");

        // Verify Attempt saved in DB
        var attemptInDb = await context.Attempts.FirstOrDefaultAsync(a => a.SessionId == session.Id);
        attemptInDb.Should().NotBeNull();
        attemptInDb!.Confidence.Should().BeNull();
        attemptInDb.FeedbackType.Should().Be("correct");
    }

    [Fact]
    public async Task ProcessPracticeAttempt_WhenFirstAttemptIncorrect_ReturnsRetryFeedback()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var child = new Child
        {
            Id = Guid.NewGuid(),
            Name = "نور",
            Age = 5,
            AvatarId = "avatar_girl"
        };
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Child = child,
            ChildId = child.Id,
            Type = "Practice",
            Status = "InProgress",
            TotalWords = 4
        };
        var word = new Word
        {
            Id = 200,
            WordText = "س",
            Type = "Letter",
            ImageUrl = "/assets/letters/seen.png"
        };
        context.Children.Add(child);
        context.Sessions.Add(session);
        context.Words.Add(word);
        await context.SaveChangesAsync();

        var audioStorageMock = new Mock<IAudioStorageService>();
        audioStorageMock.Setup(a => a.SaveAudioAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/audio/practice.wav");

        var wordVerificationMock = new Mock<IWordVerificationService>();
        wordVerificationMock.Setup(w => w.VerifyWordAsync(It.IsAny<Stream>(), "س", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WordVerificationResult { Matched = false, TranscribedText = "ش", MatchScore = 0.3 });

        var pronunciationAnalysisMock = new Mock<IPronunciationAnalysisService>();
        pronunciationAnalysisMock.Setup(p => p.AnalyzeAsync("س", "ش", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PronunciationAnalysisResult
            {
                IsCorrect = false,
                Errors = new List<PronunciationErrorDetail>
                {
                    new() { Type = "Substitution", Expected = "س", Predicted = "ش" }
                }
            });

        var loggerMock = new Mock<ILogger<AttemptProcessingService>>();
        var service = new AttemptProcessingService(
            context,
            audioStorageMock.Object,
            wordVerificationMock.Object,
            pronunciationAnalysisMock.Object,
            loggerMock.Object);

        using var fakeAudioStream = new MemoryStream(new byte[] { 4, 5, 6 });

        // Act - Attempt 1
        var result1 = await service.ProcessPracticeAttemptAsync(
            session.Id, word.Id, fakeAudioStream, "practice.wav");

        // Assert - Attempt 1 should be retry
        result1.IsCorrect.Should().BeFalse();
        result1.Feedback.Type.Should().Be("retry");

        // Act - Attempt 2 (second failure for same word)
        using var fakeAudioStream2 = new MemoryStream(new byte[] { 7, 8, 9 });
        var result2 = await service.ProcessPracticeAttemptAsync(
            session.Id, word.Id, fakeAudioStream2, "practice2.wav");

        // Assert - Attempt 2 should be needsPractice
        result2.IsCorrect.Should().BeFalse();
        result2.Feedback.Type.Should().Be("needsPractice");
    }

    [Fact]
    public async Task GetHomeScreen_WhenAssessmentCompleted_ReturnsContinuePracticeJourney()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var child = new Child
        {
            Id = Guid.NewGuid(),
            Name = "كريم",
            Age = 7,
            AvatarId = "avatar_3"
        };
        var completedAssessment = new Session
        {
            Id = Guid.NewGuid(),
            ChildId = child.Id,
            Type = "Assessment",
            Status = "Completed",
            TotalWords = 5,
            CorrectCount = 4,
            NeedsPracticeCount = 1,
            Score = 80.0
        };
        context.Children.Add(child);
        context.Sessions.Add(completedAssessment);
        await context.SaveChangesAsync();

        var service = new HomeService(context);

        // Act
        var result = await service.GetHomeScreenAsync(child.Id);

        // Assert
        result.Should().NotBeNull();
        result.Child.Name.Should().Be("كريم");
        result.Journey.SuggestedAction.Should().Be("continuePractice");
    }

    [Fact]
    public async Task GetHomeScreen_WhenNoCompletedAssessment_ReturnsStartAssessmentJourney()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var child = new Child
        {
            Id = Guid.NewGuid(),
            Name = "تامر",
            Age = 5,
            AvatarId = "avatar_1"
        };
        context.Children.Add(child);
        await context.SaveChangesAsync();

        var service = new HomeService(context);

        // Act
        var result = await service.GetHomeScreenAsync(child.Id);

        // Assert
        result.Should().NotBeNull();
        result.Journey.SuggestedAction.Should().Be("startAssessment");
    }

    [Fact]
    public async Task GetHomeScreen_WhenChildDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var nonExistentChildId = Guid.NewGuid();
        var service = new HomeService(context);

        // Act & Assert
        var act = async () => await service.GetHomeScreenAsync(nonExistentChildId);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Child*not found*");
    }

    [Fact]
    public async Task GetAssessmentWords_ReturnsFiveAssessmentWords_IncludingFoxAndTree()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AssessmentService(context);

        // Act
        var words = await service.GetAssessmentWordsAsync(5);

        // Assert
        words.Should().HaveCount(5);
        words.Select(w => w.Word).Should().ContainInOrder("كلب", "قرد", "أسد", "ثعلب", "شجرة");
        words.Select(w => w.Id).Should().ContainInOrder(5, 6, 7, 8, 9);
        words.First(w => w.Word == "ثعلب").ImageUrl.Should().Be("/assets/words/fox.png");
        words.First(w => w.Word == "شجرة").ImageUrl.Should().Be("/assets/words/tree.png");
    }
}

