using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Attempts;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Entities;
using NOTQ.Domain.Enums;

namespace NOTQ.Application.Services;

public class AttemptService : IAttemptService
{
    private readonly IApplicationDbContext _context;
    private readonly IAudioStorageService _storageService;
    private readonly ISpeechAnalysisService _analysisService;
    private readonly IScoringService _scoringService;

    public AttemptService(
        IApplicationDbContext context,
        IAudioStorageService storageService,
        ISpeechAnalysisService analysisService,
        IScoringService scoringService)
    {
        _context = context;
        _storageService = storageService;
        _analysisService = analysisService;
        _scoringService = scoringService;
    }

    public async Task<AttemptResponseDto> RecordAttemptAsync(
        Guid parentId,
        Guid sessionId,
        SubmitAttemptRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.PracticeSessions
            .Include(s => s.Child)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("PracticeSession", sessionId);
        }

        if (session.Child.ParentId != parentId)
        {
            throw new ForbiddenException("You do not have access to this session.");
        }

        if (session.Status != SessionStatus.InProgress)
        {
            throw new ConflictException("Cannot submit attempts to a session that is already completed or cancelled.");
        }

        var word = await _context.PracticeWords
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == dto.WordId, cancellationToken);

        if (word == null)
        {
            throw new NotFoundException("PracticeWord", dto.WordId);
        }

        // 1. Store audio file
        string audioUrl;
        using (var audioStream = dto.Audio.OpenReadStream())
        {
            audioUrl = await _storageService.SaveAudioAsync(audioStream, dto.Audio.FileName, cancellationToken);
        }

        // 2. Create AudioAttempt
        var attempt = new AudioAttempt
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            WordId = dto.WordId,
            AudioUrl = audioUrl,
            CreatedAt = DateTime.UtcNow
        };

        _context.AudioAttempts.Add(attempt);

        // 3. Call AI Speech Analysis
        using (var analysisStream = dto.Audio.OpenReadStream())
        {
            var analysisResult = await _analysisService.AnalyzeAsync(analysisStream, word.Word, cancellationToken);

            var resultEntity = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                Prediction = analysisResult.Prediction,
                Confidence = analysisResult.Confidence,
                IssueType = analysisResult.IssueType,
                DetectedWord = analysisResult.DetectedWord,
                CreatedAt = DateTime.UtcNow
            };

            attempt.AnalysisResult = resultEntity;
            _context.AnalysisResults.Add(resultEntity);
        }

        // 4. Update session statistics
        session.TotalAttempts++;
        if (attempt.AnalysisResult?.Prediction == PronunciationPrediction.Correct)
        {
            session.CorrectAttempts++;
        }
        session.Score = _scoringService.CalculateSessionScore(session.CorrectAttempts, session.TotalAttempts);
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // 5. Generate friendly feedback
        var feedback = GenerateFeedback(attempt.AnalysisResult?.Prediction ?? PronunciationPrediction.Incorrect);

        return new AttemptResponseDto
        {
            AttemptId = attempt.Id,
            WordId = word.Id,
            Word = word.Word,
            AudioUrl = attempt.AudioUrl,
            Prediction = attempt.AnalysisResult?.Prediction ?? PronunciationPrediction.Incorrect,
            Confidence = attempt.AnalysisResult?.Confidence ?? 0.0,
            IssueType = attempt.AnalysisResult?.IssueType ?? IssueType.None,
            DetectedWord = attempt.AnalysisResult?.DetectedWord,
            Feedback = feedback,
            CreatedAt = attempt.CreatedAt
        };
    }

    private static FeedbackDto GenerateFeedback(PronunciationPrediction prediction)
    {
        if (prediction == PronunciationPrediction.Correct)
        {
            return new FeedbackDto
            {
                Type = "Success",
                Message = "ممتاز! نطق رائع"
            };
        }

        return new FeedbackDto
        {
            Type = "Retry",
            Message = "حاول تاني!"
        };
    }
}
