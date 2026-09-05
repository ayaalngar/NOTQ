using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.DTOs.Children;
using NOTQ.Application.Services;
using NOTQ.Domain.Entities;
using NOTQ.Domain.Enums;
using NOTQ.Infrastructure.Persistence;
using Xunit;

namespace NOTQ.Tests;

public class ChildOwnershipTests
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _parentAId = Guid.NewGuid();
    private readonly Guid _parentBId = Guid.NewGuid();
    private readonly Guid _childAId = Guid.NewGuid();

    public ChildOwnershipTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Seed Parent A & Child A
        _context.Users.Add(new User
        {
            Id = _parentAId,
            Name = "Parent A",
            Email = "parenta@test.com",
            PasswordHash = "hash",
            Role = UserRole.Parent
        });

        _context.Users.Add(new User
        {
            Id = _parentBId,
            Name = "Parent B",
            Email = "parentb@test.com",
            PasswordHash = "hash",
            Role = UserRole.Parent
        });

        _context.Children.Add(new Child
        {
            Id = _childAId,
            ParentId = _parentAId,
            Name = "Child A",
            DateOfBirth = DateTime.UtcNow.AddYears(-5)
        });

        _context.SaveChanges();
    }

    [Fact]
    public async Task ParentA_CanRetrieveTheirOwnChild()
    {
        var service = new ChildService(_context);

        var child = await service.GetChildByIdAsync(_parentAId, _childAId);

        child.Should().NotBeNull();
        child.Id.Should().Be(_childAId);
        child.Name.Should().Be("Child A");
    }

    [Fact]
    public async Task ParentB_CannotAccessParentAChild_ThrowsForbiddenException()
    {
        var service = new ChildService(_context);

        var act = async () => await service.GetChildByIdAsync(_parentBId, _childAId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ParentB_CannotUpdateParentAChild_ThrowsForbiddenException()
    {
        var service = new ChildService(_context);

        var updateDto = new UpdateChildDto
        {
            Name = "Hacked Name",
            DateOfBirth = DateTime.UtcNow.AddYears(-6)
        };

        var act = async () => await service.UpdateChildAsync(_parentBId, _childAId, updateDto);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ParentB_CannotDeleteParentAChild_ThrowsForbiddenException()
    {
        var service = new ChildService(_context);

        var act = async () => await service.DeleteChildAsync(_parentBId, _childAId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
