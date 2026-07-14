using EventBoard.Api.Models;

namespace EventBoard.Api.Repositories;

public interface IUserRepository
{
    /// <summary>
    /// Adds a new user to the database and saves changes.
    /// </summary>
    Task AddUserAsync(User user);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Checks if a user with the given email already exists.
    /// </summary>
    Task<bool> UserExistsAsync(string email);
}
