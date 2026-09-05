using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.DTOs.Auth;
using NOTQ.Application.Interfaces;
using NOTQ.Application.Services;
using NOTQ.Domain.Entities;
using NOTQ.Domain.Enums;
using NOTQ.Infrastructure.Authentication;
using NOTQ.Infrastructure.Persistence;
using Xunit;

namespace NOTQ.Tests;

public class AuthTests
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher _hasher = new();
    private readonly Mock<IJwtTokenService> _jwtMock = new();

    public AuthTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _jwtMock.Setup(j => j.AccessTokenExpiryMinutes).Returns(60);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("mock_jwt_token");
        _jwtMock.Setup(j => j.GenerateRefreshToken(It.IsAny<Guid>())).Returns((Guid uid) => new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = uid,
            Token = "mock_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        });
    }

    [Fact]
    public void PasswordHasher_ShouldCorrectlyHashAndVerify()
    {
        var password = "SecurePassword123!";
        var hash = _hasher.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);

        _hasher.VerifyPassword(password, hash).Should().BeTrue();
        _hasher.VerifyPassword("WrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUserAndReturnTokens()
    {
        var authService = new AuthService(_context, _hasher, _jwtMock.Object);

        var request = new RegisterRequestDto
        {
            Name = "Ahmed Parent",
            Email = "ahmed@example.com",
            Password = "Password123!"
        };

        var response = await authService.RegisterAsync(request);

        response.Should().NotBeNull();
        response.AccessToken.Should().Be("mock_jwt_token");
        response.RefreshToken.Should().Be("mock_refresh_token");
        response.User.Email.Should().Be("ahmed@example.com");
        response.User.Name.Should().Be("Ahmed Parent");
        response.User.Role.Should().Be(UserRole.Parent);

        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "ahmed@example.com");
        dbUser.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldThrowConflictException()
    {
        var authService = new AuthService(_context, _hasher, _jwtMock.Object);

        _context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "Existing Parent",
            Email = "duplicate@example.com",
            PasswordHash = _hasher.HashPassword("Pass123!"),
            Role = UserRole.Parent
        });
        await _context.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            Name = "New Parent",
            Email = "duplicate@example.com",
            Password = "AnotherPassword!"
        };

        var act = async () => await authService.RegisterAsync(request);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldThrowUnauthorizedException()
    {
        var authService = new AuthService(_context, _hasher, _jwtMock.Object);

        _context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "Parent",
            Email = "parent@example.com",
            PasswordHash = _hasher.HashPassword("CorrectPassword"),
            Role = UserRole.Parent
        });
        await _context.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Email = "parent@example.com",
            Password = "WrongPassword"
        };

        var act = async () => await authService.LoginAsync(request);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
