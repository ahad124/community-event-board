using EventBoard.Api.Models;
using EventBoard.Api.Repositories;

namespace EventBoard.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Guid> RegisterAsync(string userName, string email, string password, string role = "User")
    {
        userName = userName.Trim();
        email = email.Trim().ToLowerInvariant();

        // Check if email already exists
        if (await _userRepository.UserExistsAsync(email))
        {
            _logger.LogWarning("Registration failed: email {Email} already exists", email);
            throw new InvalidOperationException($"A user with email '{email}' already exists.");
        }

        // Validate role
        var allowedRoles = new[] { "User", "Admin" };
        if (!allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            role = "User"; // Fallback to User if invalid role provided
        }

        // Hash the password with BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // Create a new user with the selected role
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role
        };

        await _userRepository.AddUserAsync(user);

        _logger.LogInformation("User registered successfully with ID: {UserId}, Role: {Role}", user.Id, role);
        return user.Id;
    }

    public async Task<AuthResponseDto?> LoginAsync(string email, string password)
    {
        // Look up the user by email
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            _logger.LogWarning("Login failed: no user found with email {Email}", email);
            return null;
        }

        // Verify the BCrypt password hash
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: invalid password for email {Email}", email);
            return null;
        }

        // Reject disabled accounts
        if (!user.IsActive)
        {
            _logger.LogWarning("Login blocked: account {Email} is disabled", email);
            throw new InvalidOperationException("This account has been disabled. Please contact an administrator.");
        }

        // Generate JWT token
        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        return new AuthResponseDto
{
    Token = token,
    ExpiresAt = expiresAt,
    Role = user.Role,
    UserName = user.UserName
};
    }
}
