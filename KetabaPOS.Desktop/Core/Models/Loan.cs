using KetabaPOS.Desktop.Core.Enums;
namespace KetabaPOS.Desktop.Core.Models;
public class Loan : BaseEntity
{
    public LoanType LoanType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => Amount - PaidAmount;
    public decimal InterestRate { get; set; }
    public DateTime DueDate { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public string? Notes { get; set; }
    public Customer? Customer { get; set; }
    public Supplier? Supplier { get; set; }
    public ICollection<LoanPayment> LoanPayments { get; set; } = new List<LoanPayment>();
}
