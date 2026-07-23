using KetabaPOS.Desktop.Core.Models;
namespace KetabaPOS.Desktop.Core.Interfaces;
public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<User?> GetCurrentUserAsync();
    void Logout();
}
