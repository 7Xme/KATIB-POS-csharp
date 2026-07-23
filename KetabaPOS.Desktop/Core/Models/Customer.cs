namespace KetabaPOS.Desktop.Core.Models;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal Balance { get; set; }
    public int LoyaltyPoints { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
