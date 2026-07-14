using System.Security.Claims;
using EventBoard.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersController> _logger;

    private static readonly string[] AllowedRoles = { "User", "Admin" };

    public UsersController(IUserRepository userRepository, ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>List all users (Admin only).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userRepository.GetAllAsync();
        return Ok(users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            Role = u.Role,
            IsActive = u.IsActive
        }));
    }

    /// <summary>Promote/demote a user by setting their role (Admin only).</summary>
    [HttpPut("{id}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        if (!AllowedRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Role must be either 'User' or 'Admin'.");
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound($"User {id} not found.");
        }

        // Prevent an admin from demoting their own account (avoids self-lockout).
        if (IsCurrentUser(id) && !request.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("You cannot change your own admin role.");
        }

        user.Role = AllowedRoles.First(r => r.Equals(request.Role, StringComparison.OrdinalIgnoreCase));
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} role changed to {Role}", id, user.Role);
        return Ok(new { user.Id, user.Role });
    }

    /// <summary>Enable/disable a user account (Admin only).</summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound($"User {id} not found.");
        }

        // Prevent an admin from disabling their own account.
        if (IsCurrentUser(id) && !request.IsActive)
        {
            return BadRequest("You cannot disable your own account.");
        }

        user.IsActive = request.IsActive;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} IsActive set to {IsActive}", id, request.IsActive);
        return Ok(new { user.Id, user.IsActive });
    }

    private bool IsCurrentUser(Guid id)
    {
        var current = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(current, out var currentId) && currentId == id;
    }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = "User";
}

public class UpdateStatusRequest
{
    public bool IsActive { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
