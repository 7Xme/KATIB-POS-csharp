using KetabaPOS.Desktop.Core.Models;

namespace KetabaPOS.Desktop.Core.Interfaces;

public interface ISaleService
{
    Task<Sale> CreateSaleAsync(Sale sale, List<SaleItem> items);
    Task<IEnumerable<Sale>> GetSalesAsync(DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50);
    Task<Sale?> GetSaleByIdAsync(int id);
    Task<bool> CancelSaleAsync(int id);
    Task<byte[]> GenerateReceiptAsync(int saleId);
}
