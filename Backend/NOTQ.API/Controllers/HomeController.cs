using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

[ApiController]
[Route("api/v1/home")]
public class HomeController : ControllerBase
{
    private readonly IHomeService _homeService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IHomeService homeService,
        ILogger<HomeController> logger)
    {
        _homeService = homeService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HomeScreenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHomeScreen(
        [FromQuery] Guid childId,
        CancellationToken cancellationToken = default)
    {
        if (childId == Guid.Empty)
        {
            throw new ValidationException("childId", "A valid childId is required.");
        }

        _logger.LogInformation("Requesting home screen for child {ChildId}", childId);
        var result = await _homeService.GetHomeScreenAsync(childId, cancellationToken);
        return Ok(result);
    }
}
