using Reconova.Domain.Entities.Identity;

namespace Reconova.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, Guid? sessionId = null);
    string GenerateRefreshToken();
    string HashToken(string token);
    (Guid UserId, Guid SessionId)? ValidateAccessToken(string token);
}
