namespace KetabaPOS.Desktop.Core.Models;

public class SaleItem : BaseEntity
{
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }

    public Sale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
