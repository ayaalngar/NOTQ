using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Models;
using NOTQ.Application.DTOs.Children;
using NOTQ.Application.Interfaces;

namespace NOTQ.API.Controllers;

[Authorize]
public class ChildrenController : BaseApiController
{
    private readonly IChildService _childService;

    public ChildrenController(IChildService childService)
    {
        _childService = childService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ChildResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChild([FromBody] CreateChildDto request, CancellationToken cancellationToken)
    {
        var child = await _childService.CreateChildAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetChildById), new { id = child.Id }, ApiResponse<ChildResponseDto>.Ok(child, "Child created successfully."));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ChildResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChildren(CancellationToken cancellationToken)
    {
        var children = await _childService.GetChildrenByParentAsync(CurrentUserId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<ChildResponseDto>>.Ok(children));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChildResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChildById(Guid id, CancellationToken cancellationToken)
    {
        var child = await _childService.GetChildByIdAsync(CurrentUserId, id, cancellationToken);
        return Ok(ApiResponse<ChildResponseDto>.Ok(child));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChildResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateChild(Guid id, [FromBody] UpdateChildDto request, CancellationToken cancellationToken)
    {
        var child = await _childService.UpdateChildAsync(CurrentUserId, id, request, cancellationToken);
        return Ok(ApiResponse<ChildResponseDto>.Ok(child, "Child updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteChild(Guid id, CancellationToken cancellationToken)
    {
        await _childService.DeleteChildAsync(CurrentUserId, id, cancellationToken);
        return Ok(ApiResponse.Ok("Child profile deleted successfully."));
    }
}
