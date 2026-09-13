using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

[ApiController]
[Route("api/v1/assessment")]
public class AssessmentController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;
    private readonly IAttemptProcessingService _attemptProcessingService;
    private readonly ILogger<AssessmentController> _logger;

    public AssessmentController(
        IAssessmentService assessmentService,
        IAttemptProcessingService attemptProcessingService,
        ILogger<AssessmentController> logger)
    {
        _assessmentService = assessmentService;
        _attemptProcessingService = attemptProcessingService;
        _logger = logger;
    }

    [HttpGet("words")]
    public async Task<IActionResult> GetWords([FromQuery] int count = 5, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving {Count} assessment words", count);
        var words = await _assessmentService.GetAssessmentWordsAsync(count, cancellationToken);
        return Ok(words);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting assessment session for child {ChildId}", request.ChildId);
        var session = await _assessmentService.StartAssessmentSessionAsync(request.ChildId, cancellationToken);
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
            "Submitting assessment attempt for session {SessionId}, wordId {WordId}",
            sessionId, wordId);

        using var stream = audio.OpenReadStream();
        var result = await _attemptProcessingService.ProcessAssessmentAttemptAsync(
            sessionId, wordId, stream, audio.FileName, cancellationToken);

        return Ok(result);
    }

    [HttpPost("sessions/{sessionId:guid}/complete")]
    public async Task<IActionResult> CompleteSession(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Completing assessment session {SessionId}", sessionId);
        var result = await _assessmentService.CompleteAssessmentSessionAsync(sessionId, cancellationToken);
        return Ok(result);
    }
}
