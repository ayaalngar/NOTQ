using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

[ApiController]
[Route("api/v1/children")]
public class ChildrenController : ControllerBase
{
    private readonly IMobileChildService _childService;
    private readonly ILogger<ChildrenController> _logger;

    public ChildrenController(
        IMobileChildService childService,
        ILogger<ChildrenController> logger)
    {
        _childService = childService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateChild(
        [FromBody] CreateChildMobileRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating standalone child profile for '{Name}'", request.Name);
        var response = await _childService.CreateChildAsync(request, cancellationToken);
        return Created($"/api/v1/children/{response.ChildId}", response);
    }
}
