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

    public async Task<byte[]> GenerateReceiptAsync(int saleId)
    {
        var sale = await GetSaleByIdAsync(saleId);
        if (sale == null) return [];

        var settings = await _context.Settings.ToDictionaryAsync(s => s.Key, s => s.Value);

        var companyName = settings.GetValueOrDefault("company_name", "KETABA POS");
        var companyAddress = settings.GetValueOrDefault("company_address", "");
        var companyPhone = settings.GetValueOrDefault("company_phone", "");
        var currency = settings.GetValueOrDefault("currency", "SAR");
        var receiptHeader = settings.GetValueOrDefault("receipt_header", "KETABA POS - RECEIPT");
        var receiptFooter = settings.GetValueOrDefault("receipt_footer", "Thank you for your purchase!");
        var paperFormat = settings.GetValueOrDefault("paper_format", "Thermal 80mm");

        var lineWidth = paperFormat switch
        {
            "A4" => 64, "A5" => 48, "A6" => 40, "Letter" => 64,
            _ => 32
        };

        var sep = new string('=', lineWidth);
        var dash = new string('-', lineWidth);
        var half = lineWidth / 2;

        var lines = new List<string>
        {
            sep,
            Centered(companyName, lineWidth),
            sep,
            Centered(receiptHeader, lineWidth),
            sep,
            $"Invoice: {sale.InvoiceNumber}",
            $"Date: {sale.CreatedAt:yyyy-MM-dd HH:mm}",
            $"Cashier: {sale.User?.DisplayName ?? "N/A"}",
        };

        if (!string.IsNullOrWhiteSpace(companyAddress))
            lines.Add($"Address: {companyAddress}");
        if (!string.IsNullOrWhiteSpace(companyPhone))
            lines.Add($"Phone: {companyPhone}");

        lines.Add(dash);

        var nameWidth = lineWidth - 16;
        lines.Add(PadRight("Item", nameWidth) + " Qty  Price");
        lines.Add(dash);

        foreach (var item in sale.SaleItems)
        {
            var name = (item.Product?.Name ?? "Item");
            if (name.Length > nameWidth) name = name[..nameWidth];
            lines.Add($"{PadRight(name, nameWidth)} {item.Quantity,4} {item.TotalPrice,8:F2}");
        }

        lines.Add(dash);
        lines.Add($"{PadRight("Subtotal:", 16)} {sale.Subtotal,10:F2}");
        lines.Add($"{PadRight("Tax:", 16)} {sale.TaxAmount,10:F2}");
        lines.Add($"{PadRight("Discount:", 16)} {sale.DiscountAmount,10:F2}");
        lines.Add($"{PadRight("TOTAL:", 16)} {sale.TotalAmount,10:F2}");
        lines.Add($"{PadRight("Paid:", 16)} {sale.PaidAmount,10:F2}");
        lines.Add($"{PadRight("Change:", 16)} {sale.ChangeAmount,10:F2}");
        lines.Add(sep);

        foreach (var footerLine in receiptFooter.Split('\n'))
            lines.Add(Centered(footerLine.Trim(), lineWidth));

        lines.Add(sep);

        return System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines));
    }

    private static string Centered(string text, int width)
    {
        if (string.IsNullOrEmpty(text)) return new string(' ', width);
        var pad = (width - text.Length) / 2;
        if (pad < 0) pad = 0;
        return new string(' ', pad) + text;
    }

    private static string PadRight(string text, int width)
    {
        if (text.Length >= width) return text;
        return text + new string(' ', width - text.Length);
    }
}
