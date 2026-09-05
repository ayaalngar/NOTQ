using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Models;
using NOTQ.Application.DTOs.Reports;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

[Authorize]
public class ReportsController : BaseApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("/api/v1/children/{childId:guid}/report")]
    [HttpGet("/api/v1/children/{childId:guid}/reports")]
    [ProducesResponseType(typeof(ApiResponse<ChildReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChildReport([FromRoute] Guid childId, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetChildReportAsync(CurrentUserId, childId, cancellationToken);
        return Ok(ApiResponse<ChildReportDto>.Ok(report));
    }

    [HttpGet("/api/v1/sessions/{sessionId:guid}/report")]
    [ProducesResponseType(typeof(ApiResponse<SessionReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSessionReport([FromRoute] Guid sessionId, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetSessionReportAsync(CurrentUserId, sessionId, cancellationToken);
        return Ok(ApiResponse<SessionReportDto>.Ok(report));
    }
}
