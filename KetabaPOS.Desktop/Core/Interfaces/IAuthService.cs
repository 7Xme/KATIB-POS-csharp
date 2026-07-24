using KetabaPOS.Desktop.Core.Models;
namespace KetabaPOS.Desktop.Core.Interfaces;
public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<User?> GetCurrentUserAsync();
    void Logout();
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(User user, string password);
    Task UpdateUserAsync(User user);
    Task ToggleUserActiveAsync(int userId);
}
