using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Entities;

namespace NOTQ.Application.Services;

public class AttemptProcessingService : IAttemptProcessingService
{
    private readonly IApplicationDbContext _context;
    private readonly IAudioStorageService _audioStorage;
    private readonly IWordVerificationService _wordVerificationService;
    private readonly IPronunciationAnalysisService _pronunciationAnalysisService;
    private readonly ILogger<AttemptProcessingService> _logger;

    public AttemptProcessingService(
        IApplicationDbContext context,
        IAudioStorageService audioStorage,
        IWordVerificationService wordVerificationService,
        IPronunciationAnalysisService pronunciationAnalysisService,
        ILogger<AttemptProcessingService> logger)
    {
        _context = context;
        _audioStorage = audioStorage;
        _wordVerificationService = wordVerificationService;
        _pronunciationAnalysisService = pronunciationAnalysisService;
        _logger = logger;
    }

    public async Task<SubmitAttemptResponseDto> ProcessAssessmentAttemptAsync(
        Guid sessionId,
        int wordId,
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var (attempt, isCorrect) = await ExecuteAttemptPipelineAsync(sessionId, wordId, audioStream, fileName, cancellationToken);

        return new SubmitAttemptResponseDto
        {
            AttemptId = attempt.Id,
            WordId = attempt.WordId,
            Prediction = attempt.Prediction,
            Confidence = null, // Strictly null per architecture contract
            IssueType = attempt.IssueType,
            DetectedWord = attempt.DetectedWord,
            Feedback = new AttemptFeedbackDto
            {
                Type = attempt.FeedbackType ?? "retry",
                Message = attempt.FeedbackMessage ?? string.Empty
            }
        };
    }

    public async Task<PracticeAttemptResponseDto> ProcessPracticeAttemptAsync(
        Guid sessionId,
        int wordId,
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var (attempt, isCorrect) = await ExecuteAttemptPipelineAsync(sessionId, wordId, audioStream, fileName, cancellationToken);

        return new PracticeAttemptResponseDto
        {
            IsCorrect = isCorrect,
            Feedback = new AttemptFeedbackDto
            {
                Type = attempt.FeedbackType ?? "retry",
                Message = attempt.FeedbackMessage ?? string.Empty
            }
        };
    }

    private async Task<(Attempt Attempt, bool IsCorrect)> ExecuteAttemptPipelineAsync(
        Guid sessionId,
        int wordId,
        Stream audioStream,
        string fileName,
        CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException("Session", sessionId);
        }

        if (string.Equals(session.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("session", "Session is already completed.");
        }

        var word = await _context.Words
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == wordId, cancellationToken);

        if (word == null)
        {
            throw new NotFoundException("Word", wordId);
        }

        // Buffer audio stream in memory so it can be read multiple times
        using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        // 1. Save audio file locally
        string audioUrl;
        try
        {
            audioUrl = await _audioStorage.SaveAudioAsync(memoryStream, fileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save attempt audio file");
            audioUrl = $"/uploads/audio/{Guid.NewGuid()}_{fileName}";
        }

        // 2. Transcribe via IWordVerificationService (Gradio HF Space)
        memoryStream.Position = 0;
        string? predictedText = null;
        bool isVerificationMatched = false;
        bool isVerificationUnavailable = false;

        try
        {
            var verificationResult = await _wordVerificationService.VerifyWordAsync(memoryStream, word.WordText, cancellationToken);
            if (verificationResult != null)
            {
                predictedText = verificationResult.TranscribedText;
                isVerificationMatched = verificationResult.Matched;
                isVerificationUnavailable = verificationResult.IsServiceUnavailable;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription service threw an unhandled exception for session {SessionId}, word {WordId}", sessionId, wordId);
            isVerificationUnavailable = true;
        }

        // 3. Analyze pronunciation via IPronunciationAnalysisService (Railway API)
        bool isAnalysisCorrect = false;
        string? issueType = null;
        bool isAnalysisUnavailable = false;

        try
        {
            var analysisResult = await _pronunciationAnalysisService.AnalyzeAsync(word.WordText, predictedText, confidence: null, cancellationToken);
            if (analysisResult != null)
            {
                isAnalysisCorrect = analysisResult.IsCorrect;
                isAnalysisUnavailable = analysisResult.IsServiceUnavailable;
                issueType = analysisResult.Errors?.FirstOrDefault()?.Type ?? (isAnalysisCorrect ? "None" : "Substitution");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pronunciation analysis service threw an unhandled exception for session {SessionId}, word {WordId}", sessionId, wordId);
            isAnalysisUnavailable = true;
        }

        // 4. Calculate AttemptNumber for this (session, word)
        var existingAttemptsCount = await _context.Attempts
            .CountAsync(a => a.SessionId == sessionId && a.WordId == wordId, cancellationToken);
        var attemptNumber = existingAttemptsCount + 1;

        // 5. Determine overall correctness and feedback
        bool isServiceUnavailable = isVerificationUnavailable || isAnalysisUnavailable;
        bool isCorrect = !isServiceUnavailable && (isAnalysisCorrect || isVerificationMatched);

        string? prediction;
        string feedbackType;
        string feedbackMessage;

        if (isServiceUnavailable)
        {
            prediction = null;
            feedbackType = "retry";
            feedbackMessage = "تعذر تحليل الصوت حالياً، يرجى المحاولة مرة أخرى.";
        }
        else if (isCorrect)
        {
            prediction = "correct";
            issueType = "None";
            feedbackType = "correct"; // CRITICAL: strictly lowercase "correct"
            feedbackMessage = "ممتاز! نطقك صحيح وصوتك رائع.";
        }
        else
        {
            prediction = "incorrect";
            issueType ??= "Substitution";

            if (attemptNumber == 1)
            {
                feedbackType = "retry";
                feedbackMessage = "حاول مرة أخرى! يمكنك نطقها بشكل أوضح.";
            }
            else
            {
                feedbackType = "needsPractice";
                feedbackMessage = "أحسنت المحاولة! سنتدرب عليها معاً أكثر لاحقاً.";
            }
        }

        var attempt = new Attempt
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            WordId = wordId,
            AttemptNumber = attemptNumber,
            AudioUrl = audioUrl,
            Prediction = prediction,
            Confidence = null, // Strictly null
            IssueType = issueType,
            DetectedWord = predictedText,
            FeedbackType = feedbackType,
            FeedbackMessage = feedbackMessage,
            CreatedAt = DateTime.UtcNow
        };

        _context.Attempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        return (attempt, isCorrect);
    }
}
