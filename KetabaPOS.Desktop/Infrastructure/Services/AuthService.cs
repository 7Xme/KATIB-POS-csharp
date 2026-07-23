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
}
