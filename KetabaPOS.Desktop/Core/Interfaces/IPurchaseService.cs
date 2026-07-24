using KetabaPOS.Desktop.Core.Models;
namespace KetabaPOS.Desktop.Core.Interfaces;
public interface IPurchaseService
{
    Task<IEnumerable<Purchase>> GetPurchasesAsync(string? search = null, int? supplierId = null, bool? isReceived = null, int page = 1, int pageSize = 50);
    Task<Purchase?> GetByIdAsync(int id);
    Task<Purchase> CreateAsync(Purchase purchase, List<PurchaseItem> items);
    Task ReceivePurchaseAsync(int purchaseId);
    Task CancelPurchaseAsync(int id);
    Task<int> GetTotalCountAsync(string? search = null, int? supplierId = null, bool? isReceived = null);
    Task<IEnumerable<Supplier>> GetSuppliersAsync();
    Task<IEnumerable<Product>> GetProductsAsync(string? search = null);
}
