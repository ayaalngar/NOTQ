using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

[ApiController]
[Route("api/v1/practice")]
public class PracticeController : ControllerBase
{
    private readonly IPracticeService _practiceService;
    private readonly IAttemptProcessingService _attemptProcessingService;
    private readonly ILogger<PracticeController> _logger;

    public PracticeController(
        IPracticeService practiceService,
        IAttemptProcessingService attemptProcessingService,
        ILogger<PracticeController> logger)
    {
        _practiceService = practiceService;
        _attemptProcessingService = attemptProcessingService;
        _logger = logger;
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> StartSession(
        [FromBody] StartPracticeSessionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting practice session for child {ChildId}, filter '{Filter}'",
            request.ChildId, request.CategoryFilter);

        var session = await _practiceService.StartPracticeSessionAsync(
            request.ChildId, request.CategoryFilter, cancellationToken);

        return Ok(session);
    }

    [HttpPost("sessions/{sessionId:guid}/attempts")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitAttempt(
        [FromRoute] Guid sessionId,
        IFormFile? audio,
        [FromForm] int wordId,
        CancellationToken cancellationToken = default)
    {
        if (audio == null || audio.Length == 0)
        {
            throw new ValidationException("audio", "Audio file is required.");
        }

        if (wordId <= 0)
        {
            throw new ValidationException("wordId", "Valid wordId is required.");
        }

        _logger.LogInformation(
            "Submitting practice attempt for session {SessionId}, wordId {WordId}",
            sessionId, wordId);

        using var stream = audio.OpenReadStream();
        var result = await _attemptProcessingService.ProcessPracticeAttemptAsync(
            sessionId, wordId, stream, audio.FileName, cancellationToken);

        return Ok(result);
    }

    [HttpPost("sessions/{sessionId:guid}/complete")]
    public async Task<IActionResult> CompleteSession(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Completing practice session {SessionId}", sessionId);
        var result = await _practiceService.CompletePracticeSessionAsync(sessionId, cancellationToken);
        return Ok(result);
    }
}
