using KetabaPOS.Desktop.Core.Models;
namespace KetabaPOS.Desktop.Core.Interfaces;
public interface IProductService
{
    Task<IEnumerable<Product>> GetProductsAsync(string? search = null, int? categoryId = null, int page = 1, int pageSize = 50);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task<int> GetTotalCountAsync(string? search = null, int? categoryId = null);
    Task<List<Category>> GetCategoriesAsync();
    Task<List<Customer>> GetCustomersAsync();
}
