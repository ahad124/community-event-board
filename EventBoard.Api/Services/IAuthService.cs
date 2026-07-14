using EventBoard.Api.Models;

namespace EventBoard.Api.Services;

public interface IAuthService
{
    /// <summary>
    /// Registers a new user. Returns the user's ID if successful.
    /// Throws InvalidOperationException if the email is already taken.
    /// </summary>
    Task<Guid> RegisterAsync(string userName, string email, string password, string role = "User");

    /// <summary>
    /// Authenticates a user and returns a JWT token with expiry.
    /// Returns null if credentials are invalid.
    /// </summary>
    Task<AuthResponseDto?> LoginAsync(string email, string password);
}
