using KetabaPOS.Desktop.Core.Enums;

namespace KetabaPOS.Desktop.Core.Models;

public class Sale : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public int UserId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public SaleStatus Status { get; set; } = SaleStatus.Completed;
    public string? Notes { get; set; }

    public Customer? Customer { get; set; }
    public User User { get; set; } = null!;
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
