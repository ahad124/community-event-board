using EventBoard.Api.Models;
using EventBoard.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Requested role: {Role}", request.Role);

            var userId = await _authService.RegisterAsync(request.UserName, request.Email, request.Password, request.Role);
            _logger.LogInformation("User registered successfully: {UserId}", userId);
            return Ok(new { UserId = userId, Message = "Registration successful" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration conflict: {Message}", ex.Message);
            return Conflict(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        AuthResponseDto? result;
        try
        {
            result = await _authService.LoginAsync(request.Email, request.Password);
        }
        catch (InvalidOperationException ex)
        {
            // Raised when the account is disabled — treat as an authentication failure (401).
            _logger.LogWarning("Login blocked for email {Email}: {Message}", request.Email, ex.Message);
            return Unauthorized(new { Message = ex.Message });
        }

        if (result == null)
        {
            _logger.LogWarning("Login failed for email: {Email}", request.Email);
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        _logger.LogInformation("Login successful for email: {Email}", request.Email);
        return Ok(result);
    }
}

/// <summary>
/// DTO for registration request
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "UserName is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "UserName must be between 2 and 100 characters")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [StringLength(50)]
    public string Role { get; set; } = "User";
}

/// <summary>
/// DTO for login request
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}
