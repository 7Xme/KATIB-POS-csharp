using KetabaPOS.Desktop.Core.Enums;

namespace KetabaPOS.Desktop.Core.Models;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Cashier;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
