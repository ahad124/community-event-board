using EventBoard.Api.Data;
using EventBoard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventBoard.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
}
