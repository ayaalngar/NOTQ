using Microsoft.EntityFrameworkCore;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Interfaces;
using NOTQ.Application.DTOs.Auth;
using NOTQ.Application.Interfaces;
using NOTQ.Domain.Entities;

namespace NOTQ.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default)
    {
        // AUTH INFRASTRUCTURE — UNWIRED (Preserved for future parent account layer)
        throw new NotSupportedException("Parent authentication is inactive in the child-account model.");

        /*
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException($"User with email '{dto.Email}' already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtTokenService.AccessTokenExpiryMinutes),
            User = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = "Parent"
            }
        };
        */
    }

    public Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        // AUTH INFRASTRUCTURE — UNWIRED (Preserved for future parent account layer)
        throw new NotSupportedException("Parent authentication is inactive in the child-account model.");

        /*
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtTokenService.AccessTokenExpiryMinutes),
            User = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = "Parent"
            }
        };
        */
    }

    public Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Refresh token table is removed in lean schema.");
    }

    public Task<UserProfileDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // AUTH INFRASTRUCTURE — UNWIRED (Preserved for future parent account layer)
        throw new NotSupportedException("Parent authentication is inactive in the child-account model.");

        /*
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = "Parent"
        };
        */
    }
}
