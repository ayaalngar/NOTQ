using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Models;
using NOTQ.Application.DTOs.Progress;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

[Authorize]
public class ProgressController : BaseApiController
{
    private readonly IProgressService _progressService;

    public ProgressController(IProgressService progressService)
    {
        _progressService = progressService;
    }

    [HttpGet("/api/v1/children/{childId:guid}/progress")]
    [ProducesResponseType(typeof(ApiResponse<ChildProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChildProgress([FromRoute] Guid childId, CancellationToken cancellationToken)
    {
        var progress = await _progressService.GetChildProgressAsync(CurrentUserId, childId, cancellationToken);
        return Ok(ApiResponse<ChildProgressDto>.Ok(progress));
    }
}
