namespace KetabaPOS.Desktop.Core.Models;
public class Purchase : BaseEntity
{
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Notes { get; set; }
    public bool IsReceived { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
}
