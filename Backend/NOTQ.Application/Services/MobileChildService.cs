using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Mobile;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Entities;

namespace NOTQ.Application.Services;

public class MobileChildService : IMobileChildService
{
    private readonly IApplicationDbContext _context;

    public MobileChildService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateChildMobileResponseDto> CreateChildAsync(
        CreateChildMobileRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationException("name", "Child name is required.");
        }

        if (dto.Age <= 0)
        {
            throw new ValidationException("age", "Child age must be greater than 0.");
        }

        var child = new Child
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Age = dto.Age,
            AvatarId = string.IsNullOrWhiteSpace(dto.Avatar) ? "default" : dto.Avatar.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Children.Add(child);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateChildMobileResponseDto
        {
            ChildId = child.Id,
            Name = child.Name,
            Age = child.Age,
            Avatar = child.AvatarId
        };
    }
}
