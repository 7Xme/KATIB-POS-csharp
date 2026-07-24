using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
using Microsoft.EntityFrameworkCore;
using KetabaPOS.Desktop.Infrastructure.Data;
namespace KetabaPOS.Desktop.Infrastructure.Services;
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private User? _currentUser;
    public AuthService(AppDbContext context) { _context = context; }
    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        user.LastLogin = DateTime.UtcNow; await _context.SaveChangesAsync(); _currentUser = user; return user;
    }
    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash)) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword); user.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync(); return true;
    }
    public Task<User?> GetCurrentUserAsync() => Task.FromResult(_currentUser);
    public void Logout() => _currentUser = null;
    public async Task<List<User>> GetAllUsersAsync() => await _context.Users.Where(u => !u.IsDeleted).OrderBy(u => u.Username).ToListAsync();
    public async Task<User> CreateUserAsync(User user, string password)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
    public async Task UpdateUserAsync(User user) { user.UpdatedAt = DateTime.UtcNow; _context.Users.Update(user); await _context.SaveChangesAsync(); }
    public async Task ToggleUserActiveAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null) { user.IsActive = !user.IsActive; user.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync(); }
    }
}
