using FluentAssertions;
using NOTQ.Infrastructure.Authentication;
using Xunit;

namespace NOTQ.Tests;

public class AuthTests
{
    private readonly PasswordHasher _hasher = new();

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
}

