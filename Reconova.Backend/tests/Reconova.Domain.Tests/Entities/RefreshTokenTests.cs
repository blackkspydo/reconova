using FluentAssertions;
using Reconova.Domain.Entities.Identity;
using Xunit;

namespace Reconova.Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void IsExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenNotRevokedNotUsedNotExpired_ReturnsTrue()
    {
        var token = new RefreshToken
        {
            IsRevoked = false,
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            IsRevoked = true,
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenUsed_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            IsRevoked = false,
            IsUsed = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        token.IsActive.Should().BeFalse();
    }
}
