using KetabaPOS.Desktop.Core.Enums;

namespace KetabaPOS.Desktop.Core.Models;

public class InventoryTransaction : BaseEntity
{
    public int ProductId { get; set; }
    public TransactionType Type { get; set; }
    public double QuantityChange { get; set; }
    public double QuantityBefore { get; set; }
    public double QuantityAfter { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }

    public Product Product { get; set; } = null!;
}
