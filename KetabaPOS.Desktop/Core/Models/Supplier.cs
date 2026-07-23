namespace KetabaPOS.Desktop.Core.Models;
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal Balance { get; set; }
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
