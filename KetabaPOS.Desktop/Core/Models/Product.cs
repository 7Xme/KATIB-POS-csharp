namespace KetabaPOS.Desktop.Core.Models;
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal CostPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal UnitPrice { get; set; }
    public double StockQuantity { get; set; }
    public double MinStockLevel { get; set; }
    public string? ImagePath { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public bool IsActive { get; set; } = true;
    public Category Category { get; set; } = null!;
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
