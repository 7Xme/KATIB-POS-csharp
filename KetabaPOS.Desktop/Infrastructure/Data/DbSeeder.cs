using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Models;
using Microsoft.EntityFrameworkCore;
namespace KetabaPOS.Desktop.Infrastructure.Data;
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync()) return;
        var admin = new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), DisplayName = "Administrator", Role = UserRole.Admin, IsActive = true, CreatedAt = DateTime.UtcNow };
        context.Users.Add(admin);
        context.Categories.AddRange(
            new Category { Name = "Beverages", NameAr = "مشروبات" },
            new Category { Name = "Food", NameAr = "مواد غذائية" },
            new Category { Name = "Electronics", NameAr = "إلكترونيات" }
        );
        context.Products.AddRange(
            new Product { Name = "Water Bottle 1L", NameAr = "ماء 1 لتر", Barcode = "100001", SKU = "WTR001", CategoryId = 1, CostPrice = 0.5m, RetailPrice = 1.0m, WholesalePrice = 0.75m, UnitPrice = 1.0m, StockQuantity = 500, MinStockLevel = 50, Unit = "Piece", IsActive = true },
            new Product { Name = "Cola Can", NameAr = "كولا علبة", Barcode = "100002", SKU = "COLA001", CategoryId = 1, CostPrice = 0.3m, RetailPrice = 0.75m, WholesalePrice = 0.55m, UnitPrice = 0.75m, StockQuantity = 300, MinStockLevel = 30, Unit = "Piece", IsActive = true },
            new Product { Name = "Rice 5kg", NameAr = "أرز 5 كجم", Barcode = "200001", SKU = "RCE001", CategoryId = 2, CostPrice = 3.0m, RetailPrice = 5.0m, WholesalePrice = 4.0m, UnitPrice = 5.0m, StockQuantity = 100, MinStockLevel = 10, Unit = "Bag", IsActive = true },
            new Product { Name = "USB Cable 1m", NameAr = "كابل USB 1 م", Barcode = "300001", SKU = "USB001", CategoryId = 3, CostPrice = 1.0m, RetailPrice = 3.0m, WholesalePrice = 2.0m, UnitPrice = 3.0m, StockQuantity = 5, MinStockLevel = 20, Unit = "Piece", IsActive = true }
        );
        context.Customers.Add(new Customer { Name = "Walk-in Customer", NameAr = "زبون عادي", Phone = "0500000000", CreditLimit = 1000, Balance = 0 });
        context.Suppliers.Add(new Supplier { Name = "Main Supplier", NameAr = "المورد الرئيسي", Phone = "0511111111", Balance = 0 });
        context.Settings.AddRange(
            new Setting { Key = "company_name", Value = "Ketaba POS", Group = "Company" },
            new Setting { Key = "company_name_ar", Value = "كتابة نقاط البيع", Group = "Company" },
            new Setting { Key = "tax_rate", Value = "15", Group = "Tax" },
            new Setting { Key = "language", Value = "en", Group = "Localization" },
            new Setting { Key = "theme", Value = "Light", Group = "Appearance" }
        );
        await context.SaveChangesAsync();
    }
}
