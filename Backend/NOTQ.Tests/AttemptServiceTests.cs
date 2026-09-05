using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.DTOs.Analysis;
using NOTQ.Application.DTOs.Attempts;
using NOTQ.Application.Interfaces;
using NOTQ.Application.Services;
using NOTQ.Domain.Entities;
using NOTQ.Domain.Enums;
using NOTQ.Infrastructure.Persistence;
using NOTQ.Infrastructure.Services;
using Xunit;

namespace NOTQ.Tests;

public class AttemptServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAudioStorageService> _storageMock = new();
    private readonly Mock<ISpeechAnalysisService> _aiMock = new();
    private readonly IScoringService _scoring = new ScoringService();
    private readonly Guid _parentId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public AttemptServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var parent = new User
        {
            Id = _parentId,
            Name = "Parent",
            Email = "parent@test.com",
            PasswordHash = "hash"
        };
        var child = new Child
        {
            Id = _childId,
            ParentId = _parentId,
            Name = "Child"
        };
        var session = new PracticeSession
        {
            Id = _sessionId,
            ChildId = _childId,
            Status = SessionStatus.InProgress,
            Child = child
        };
        var word = new PracticeWord
        {
            Id = 1,
            Word = "سمكة",
            TargetSound = "س"
        };

        _context.Users.Add(parent);
        _context.Children.Add(child);
        _context.PracticeSessions.Add(session);
        _context.PracticeWords.Add(word);
        _context.SaveChanges();

        _storageMock.Setup(s => s.SaveAudioAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/audio/2026/09/sample.wav");
    }

    [Fact]
    public async Task SubmitAttempt_WithValidAudio_ShouldRecordAttemptAndAnalysis()
    {
        _aiMock.Setup(a => a.AnalyzeAsync(It.IsAny<Stream>(), "سمكة", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeechAnalysisResult
            {
                Prediction = PronunciationPrediction.Incorrect,
                Confidence = 0.87,
                IssueType = IssueType.Substitution,
                DetectedWord = "تمكة"
            });

        var service = new AttemptService(_context, _storageMock.Object, _aiMock.Object, _scoring);

        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("test.wav");
        fileMock.Setup(f => f.Length).Returns(3);

        var request = new SubmitAttemptRequestDto
        {
            Audio = fileMock.Object,
            WordId = 1
        };

        var response = await service.RecordAttemptAsync(_parentId, _sessionId, request);

        response.Should().NotBeNull();
        response.Prediction.Should().Be(PronunciationPrediction.Incorrect);
        response.Confidence.Should().Be(0.87);
        response.IssueType.Should().Be(IssueType.Substitution);
        response.DetectedWord.Should().Be("تمكة");
        response.Feedback.Type.Should().Be("Retry");

        // Verify session stats updated
        var session = await _context.PracticeSessions.FindAsync(_sessionId);
        session!.TotalAttempts.Should().Be(1);
        session.CorrectAttempts.Should().Be(0);
        session.Score.Should().Be(0.0);
    }

    [Fact]
    public async Task SubmitAttempt_WhenSessionIsCompleted_ShouldThrowConflictException()
    {
        var session = await _context.PracticeSessions.FindAsync(_sessionId);
        session!.Status = SessionStatus.Completed;
        await _context.SaveChangesAsync();

        var service = new AttemptService(_context, _storageMock.Object, _aiMock.Object, _scoring);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1 }));
        fileMock.Setup(f => f.FileName).Returns("test.wav");

        var request = new SubmitAttemptRequestDto
        {
            Audio = fileMock.Object,
            WordId = 1
        };

        var act = async () => await service.RecordAttemptAsync(_parentId, _sessionId, request);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task SubmitAttempt_WhenAiServiceFails_ShouldPropagateAiServiceUnavailableException()
    {
        _aiMock.Setup(a => a.AnalyzeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AiServiceUnavailableException("Speech analysis service is currently unreachable."));

        var service = new AttemptService(_context, _storageMock.Object, _aiMock.Object, _scoring);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1 }));
        fileMock.Setup(f => f.FileName).Returns("test.wav");

        var request = new SubmitAttemptRequestDto
        {
            Audio = fileMock.Object,
            WordId = 1
        };

        var act = async () => await service.RecordAttemptAsync(_parentId, _sessionId, request);

        await act.Should().ThrowAsync<AiServiceUnavailableException>();
    }
}
