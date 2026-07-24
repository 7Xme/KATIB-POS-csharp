using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace KetabaPOS.Desktop.Infrastructure.Services;
public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _context;
    public PurchaseService(AppDbContext context) { _context = context; }
    public async Task<IEnumerable<Purchase>> GetPurchasesAsync(string? search = null, int? supplierId = null, bool? isReceived = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Purchases.Include(p => p.Supplier).Include(p => p.PurchaseItems).ThenInclude(pi => pi.Product).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.PurchaseOrderNumber.Contains(search));
        if (supplierId.HasValue) query = query.Where(p => p.SupplierId == supplierId.Value);
        if (isReceived.HasValue) query = query.Where(p => p.IsReceived == isReceived.Value);
        return await query.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }
    public async Task<Purchase?> GetByIdAsync(int id) => await _context.Purchases.Include(p => p.Supplier).Include(p => p.PurchaseItems).ThenInclude(pi => pi.Product).FirstOrDefaultAsync(p => p.Id == id);
    public async Task<Purchase> CreateAsync(Purchase purchase, List<PurchaseItem> items)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var shortId = Guid.NewGuid().ToString("N")[..6].ToUpper();
            purchase.PurchaseOrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{shortId}";
            purchase.CreatedAt = DateTime.UtcNow;
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();
            foreach (var item in items)
            {
                item.PurchaseId = purchase.Id;
                item.TotalCost = item.UnitCost * (decimal)item.Quantity;
                _context.PurchaseItems.Add(item);
            }
            purchase.Subtotal = items.Sum(i => i.TotalCost);
            purchase.TotalAmount = purchase.Subtotal + purchase.TaxAmount;
            purchase.PaidAmount = 0;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return purchase;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }
    public async Task ReceivePurchaseAsync(int purchaseId)
    {
        var purchase = await _context.Purchases.Include(p => p.PurchaseItems).FirstOrDefaultAsync(p => p.Id == purchaseId);
        if (purchase == null || purchase.IsReceived) return;
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in purchase.PurchaseItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var qtyBefore = product.StockQuantity;
                    product.StockQuantity += item.Quantity;
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = item.ProductId,
                        Type = TransactionType.Purchase,
                        QuantityChange = item.Quantity,
                        QuantityBefore = qtyBefore,
                        QuantityAfter = product.StockQuantity,
                        ReferenceNumber = purchase.PurchaseOrderNumber,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            purchase.IsReceived = true;
            purchase.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }
    public async Task CancelPurchaseAsync(int id)
    {
        var purchase = await _context.Purchases.Include(p => p.PurchaseItems).FirstOrDefaultAsync(p => p.Id == id);
        if (purchase == null) return;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (purchase.IsReceived)
            {
                foreach (var item in purchase.PurchaseItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        var qtyBefore = product.StockQuantity;
                        product.StockQuantity -= item.Quantity;
                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            ProductId = item.ProductId,
                            Type = TransactionType.Adjustment,
                            QuantityChange = -item.Quantity,
                            QuantityBefore = qtyBefore,
                            QuantityAfter = product.StockQuantity,
                            ReferenceNumber = $"CANCEL-{purchase.PurchaseOrderNumber}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            purchase.IsDeleted = true;
            purchase.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<int> GetTotalCountAsync(string? search = null, int? supplierId = null, bool? isReceived = null)
    {
        var query = _context.Purchases.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.PurchaseOrderNumber.Contains(search));
        if (supplierId.HasValue) query = query.Where(p => p.SupplierId == supplierId.Value);
        if (isReceived.HasValue) query = query.Where(p => p.IsReceived == isReceived.Value);
        return await query.CountAsync();
    }
    public async Task<IEnumerable<Supplier>> GetSuppliersAsync() => await _context.Suppliers.Where(s => !s.IsDeleted).OrderBy(s => s.Name).ToListAsync();
    public async Task<IEnumerable<Product>> GetProductsAsync(string? search = null)
    {
        var query = _context.Products.Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Name.Contains(search) || p.NameAr.Contains(search) || p.Barcode.Contains(search));
        return await query.OrderBy(p => p.Name).Take(50).ToListAsync();
    }
}
