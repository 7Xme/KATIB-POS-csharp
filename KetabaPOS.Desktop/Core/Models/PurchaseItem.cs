namespace KetabaPOS.Desktop.Core.Models;
public class PurchaseItem : BaseEntity
{
    public int PurchaseId { get; set; }
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
