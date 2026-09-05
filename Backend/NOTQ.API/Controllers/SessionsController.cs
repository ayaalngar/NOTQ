using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Models;
using NOTQ.Application.DTOs.Sessions;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

[Authorize]
public class SessionsController : BaseApiController
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SessionResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartSession([FromBody] StartSessionDto request, CancellationToken cancellationToken)
    {
        var session = await _sessionService.StartSessionAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetSessionById), new { id = session.SessionId }, ApiResponse<SessionResponseDto>.Ok(session, "Session started successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SessionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessionById(Guid id, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetSessionByIdAsync(CurrentUserId, id, cancellationToken);
        return Ok(ApiResponse<SessionResponseDto>.Ok(session));
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<CompleteSessionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CompleteSession(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sessionService.CompleteSessionAsync(CurrentUserId, id, cancellationToken);
        return Ok(ApiResponse<CompleteSessionResponseDto>.Ok(result, "Session completed successfully."));
    }

    [HttpGet("child/{childId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SessionResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessionsByChild(Guid childId, CancellationToken cancellationToken)
    {
        var sessions = await _sessionService.GetSessionsByChildAsync(CurrentUserId, childId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<SessionResponseDto>>.Ok(sessions));
    }
}
