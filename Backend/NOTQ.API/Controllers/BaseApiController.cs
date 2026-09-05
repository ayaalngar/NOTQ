using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NOTQ.Application.Common.Exceptions;

namespace NOTQ.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim == null || !Guid.TryParse(claim.Value, out var userId))
            {
                throw new UnauthorizedException("User ID could not be identified from authentication token.");
            }
            return userId;
        }
    }
}
