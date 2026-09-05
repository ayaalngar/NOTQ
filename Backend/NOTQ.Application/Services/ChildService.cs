using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Children;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Entities;

namespace NOTQ.Application.Services;

public class ChildService : IChildService
{
    private readonly IApplicationDbContext _context;

    public ChildService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChildResponseDto> CreateChildAsync(Guid parentId, CreateChildDto dto, CancellationToken cancellationToken = default)
    {
        var parentExists = await _context.Users.AnyAsync(u => u.Id == parentId, cancellationToken);
        if (!parentExists)
        {
            throw new UnauthorizedException("Authenticated parent account not found.");
        }

        var child = new Child
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Name = dto.Name.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Children.Add(child);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(child);
    }

    public async Task<IEnumerable<ChildResponseDto>> GetChildrenByParentAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var children = await _context.Children
            .AsNoTracking()
            .Where(c => c.ParentId == parentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return children.Select(MapToDto);
    }

    public async Task<ChildResponseDto> GetChildByIdAsync(Guid parentId, Guid childId, CancellationToken cancellationToken = default)
    {
        var child = await _context.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId, cancellationToken);

        if (child == null)
        {
            throw new NotFoundException("Child", childId);
        }

        if (child.ParentId != parentId)
        {
            throw new ForbiddenException("You do not have access to this child's profile.");
        }

        return MapToDto(child);
    }

    public async Task<ChildResponseDto> UpdateChildAsync(Guid parentId, Guid childId, UpdateChildDto dto, CancellationToken cancellationToken = default)
    {
        var child = await _context.Children
            .FirstOrDefaultAsync(c => c.Id == childId, cancellationToken);

        if (child == null)
        {
            throw new NotFoundException("Child", childId);
        }

        if (child.ParentId != parentId)
        {
            throw new ForbiddenException("You do not have permission to modify this child's profile.");
        }

        child.Name = dto.Name.Trim();
        child.DateOfBirth = dto.DateOfBirth;
        child.Gender = dto.Gender?.Trim();
        child.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(child);
    }

    public async Task<bool> DeleteChildAsync(Guid parentId, Guid childId, CancellationToken cancellationToken = default)
    {
        var child = await _context.Children
            .FirstOrDefaultAsync(c => c.Id == childId, cancellationToken);

        if (child == null)
        {
            throw new NotFoundException("Child", childId);
        }

        if (child.ParentId != parentId)
        {
            throw new ForbiddenException("You do not have permission to delete this child's profile.");
        }

        _context.Children.Remove(child);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static ChildResponseDto MapToDto(Child child)
    {
        var age = DateTime.UtcNow.Year - child.DateOfBirth.Year;
        if (child.DateOfBirth > DateTime.UtcNow.AddYears(-age))
        {
            age--;
        }

        return new ChildResponseDto
        {
            Id = child.Id,
            ParentId = child.ParentId,
            Name = child.Name,
            DateOfBirth = child.DateOfBirth,
            Gender = child.Gender,
            AgeYears = Math.Max(0, age),
            CreatedAt = child.CreatedAt
        };
    }
}
