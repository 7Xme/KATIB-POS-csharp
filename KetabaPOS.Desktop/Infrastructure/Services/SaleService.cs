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
            var invoiceCount = await _context.Sales.CountAsync();
            sale.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{invoiceCount + 1:D4}";
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
        return true;
    }

    public async Task<byte[]> GenerateReceiptAsync(int saleId)
    {
        var sale = await GetSaleByIdAsync(saleId);
        if (sale == null) return [];

        var lines = new List<string>
        {
            "=================================",
            "       KETABA POS - RECEIPT      ",
            "=================================",
            $"Invoice: {sale.InvoiceNumber}",
            $"Date: {sale.CreatedAt:yyyy-MM-dd HH:mm}",
            $"Cashier: {sale.User?.DisplayName ?? "N/A"}",
            "---------------------------------",
            "Item                Qty    Price",
            "---------------------------------"
        };

        foreach (var item in sale.SaleItems)
        {
            var name = (item.Product?.Name ?? "Item")[..Math.Min(16, item.Product?.Name.Length ?? 16)];
            lines.Add($"{name,-16} {item.Quantity,4} {item.TotalPrice,8:F2}");
        }

        lines.Add("---------------------------------");
        lines.Add($"Subtotal:           {sale.Subtotal,10:F2}");
        lines.Add($"Tax:                {sale.TaxAmount,10:F2}");
        lines.Add($"Discount:           {sale.DiscountAmount,10:F2}");
        lines.Add($"TOTAL:              {sale.TotalAmount,10:F2}");
        lines.Add($"Paid:               {sale.PaidAmount,10:F2}");
        lines.Add($"Change:             {sale.ChangeAmount,10:F2}");
        lines.Add("=================================");
        lines.Add("      Thank you for your        ");
        lines.Add("           purchase!             ");
        lines.Add("=================================");

        return System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines));
    }
}
