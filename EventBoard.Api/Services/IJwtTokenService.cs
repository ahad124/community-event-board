using EventBoard.Api.Models;

namespace EventBoard.Api.Services;

public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT for the given user containing their Id, email, and role as claims.
    /// </summary>
    /// <returns>A tuple of the JWT string and its expiration DateTime.</returns>
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
