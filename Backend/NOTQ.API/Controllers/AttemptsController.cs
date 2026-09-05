using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Models;
using NOTQ.Application.DTOs.Attempts;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

[Authorize]
public class AttemptsController : BaseApiController
{
    private readonly IAttemptService _attemptService;

    public AttemptsController(IAttemptService attemptService)
    {
        _attemptService = attemptService;
    }

    [HttpPost("/api/v1/sessions/{sessionId:guid}/attempts")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<AttemptResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SubmitAttempt(
        [FromRoute] Guid sessionId,
        [FromForm] SubmitAttemptRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _attemptService.RecordAttemptAsync(CurrentUserId, sessionId, request, cancellationToken);
        return Ok(ApiResponse<AttemptResponseDto>.Ok(result, "Attempt recorded and analyzed successfully."));
    }
}
