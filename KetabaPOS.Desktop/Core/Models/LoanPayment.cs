namespace KetabaPOS.Desktop.Core.Models;
public class LoanPayment : BaseEntity
{
    public int LoanId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Notes { get; set; }
    public Loan Loan { get; set; } = null!;
}
