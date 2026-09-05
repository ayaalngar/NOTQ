using NOTQ.Domain.Entities;

namespace NOTQ.Application.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
    int AccessTokenExpiryMinutes { get; }
}
