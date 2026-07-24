using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KetabaPOS.Desktop.Infrastructure.Services;

public class SaleService : ISaleService
{
    private readonly AppDbContext _context;
    public SaleService(AppDbContext context) { _context = context; }

    public async Task<Sale> CreateSaleAsync(Sale sale, List<SaleItem> items)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var shortId = Guid.NewGuid().ToString("N")[..6].ToUpper();
            sale.InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{shortId}";
            sale.CreatedAt = DateTime.UtcNow;
            sale.Status = SaleStatus.Completed;
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            foreach (var item in items)
            {
                item.SaleId = sale.Id;
                item.TotalPrice = (decimal)((double)item.UnitPrice * item.Quantity) - item.DiscountAmount;
                _context.SaleItems.Add(item);

                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var quantityBefore = product.StockQuantity;
                    product.StockQuantity -= item.Quantity;
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = item.ProductId,
                        Type = TransactionType.Sale,
                        QuantityChange = -item.Quantity,
                        QuantityBefore = quantityBefore,
                        QuantityAfter = product.StockQuantity,
                        ReferenceNumber = sale.InvoiceNumber,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (sale.CustomerId.HasValue)
            {
                var c = await _context.Customers.FindAsync(sale.CustomerId.Value);
                if (c != null) c.Balance += sale.TotalAmount - sale.PaidAmount;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return sale;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Sale>> GetSalesAsync(DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Sales
            .Include(s => s.User).Include(s => s.Customer)
            .Include(s => s.SaleItems).ThenInclude(si => si.Product)
            .AsQueryable();
        if (from.HasValue) query = query.Where(s => s.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(s => s.CreatedAt <= to.Value);
        return await query.OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
    }

    public async Task<Sale?> GetSaleByIdAsync(int id) => await _context.Sales
        .Include(s => s.User).Include(s => s.Customer)
        .Include(s => s.SaleItems).ThenInclude(si => si.Product)
        .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<bool> CancelSaleAsync(int id)
    {
        var sale = await _context.Sales.Include(s => s.SaleItems).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null || sale.Status == SaleStatus.Cancelled) return false;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            sale.Status = SaleStatus.Cancelled;
            sale.UpdatedAt = DateTime.UtcNow;

            foreach (var item in sale.SaleItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var quantityBefore = product.StockQuantity;
                    product.StockQuantity += item.Quantity;
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = item.ProductId,
                        Type = TransactionType.Adjustment,
                        QuantityChange = item.Quantity,
                        QuantityBefore = quantityBefore,
                        QuantityAfter = product.StockQuantity,
                        ReferenceNumber = $"CANCEL-{sale.InvoiceNumber}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SalesSummary> GetSalesSummaryAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Sales.Where(s => s.Status == SaleStatus.Completed).AsQueryable();
        if (from.HasValue) query = query.Where(s => s.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(s => s.CreatedAt <= to.Value);
        var totalTransactions = await query.CountAsync();
        var totalSales = await query.SumAsync(s => s.TotalAmount);
        var totalTax = await query.SumAsync(s => s.TaxAmount);
        var totalDiscounts = await query.SumAsync(s => s.DiscountAmount);
        return new SalesSummary
        {
            TotalTransactions = totalTransactions,
            TotalSales = totalSales,
            TotalTax = totalTax,
            TotalDiscounts = totalDiscounts,
            AverageTransactionValue = totalTransactions > 0 ? totalSales / totalTransactions : 0
        };
    }

    public async Task<bool> RefundSaleAsync(int saleId, string? reason = null)
    {
        var sale = await _context.Sales.Include(s => s.SaleItems).FirstOrDefaultAsync(s => s.Id == saleId);
        if (sale == null || sale.Status != SaleStatus.Completed) return false;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            sale.Status = SaleStatus.Refunded;
            sale.Notes = string.IsNullOrWhiteSpace(reason) ? "Refunded" : $"Refunded: {reason}";
            sale.UpdatedAt = DateTime.UtcNow;

            foreach (var item in sale.SaleItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var qtyBefore = product.StockQuantity;
                    product.StockQuantity += item.Quantity;
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = item.ProductId,
                        Type = TransactionType.Return,
                        QuantityChange = item.Quantity,
                        QuantityBefore = qtyBefore,
                        QuantityAfter = product.StockQuantity,
                        ReferenceNumber = $"REFUND-{sale.InvoiceNumber}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

}
