using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KetabaPOS.Desktop.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Users.AnyAsync())
            return;

        var admin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            DisplayName = "Administrator",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(admin);

        var categories = new List<Category>
        {
            new() { Name = "Beverages", NameAr = "مشروبات" },
            new() { Name = "Food", NameAr = "مواد غذائية" },
            new() { Name = "Electronics", NameAr = "إلكترونيات" },
            new() { Name = "Clothing", NameAr = "ملابس" },
            new() { Name = "Cleaning", NameAr = "منظفات" }
        };
        context.Categories.AddRange(categories);

        var products = new List<Product>
        {
            new() { Name = "Water Bottle 1L", NameAr = "ماء 1 لتر", Barcode = "100001", SKU = "WTR001", CategoryId = 1, CostPrice = 0.5m, RetailPrice = 1.0m, WholesalePrice = 0.75m, StockQuantity = 500, MinStockLevel = 50, Unit = "Piece", IsActive = true },
            new() { Name = "Cola Can", NameAr = "كولا علبة", Barcode = "100002", SKU = "COLA001", CategoryId = 1, CostPrice = 0.3m, RetailPrice = 0.75m, WholesalePrice = 0.55m, StockQuantity = 300, MinStockLevel = 30, Unit = "Piece", IsActive = true },
            new() { Name = "Rice 5kg", NameAr = "أرز 5 كجم", Barcode = "200001", SKU = "RCE001", CategoryId = 2, CostPrice = 3.0m, RetailPrice = 5.0m, WholesalePrice = 4.0m, StockQuantity = 100, MinStockLevel = 10, Unit = "Bag", IsActive = true },
            new() { Name = "Cooking Oil 1L", NameAr = "زيت طهي 1 لتر", Barcode = "200002", SKU = "OIL001", CategoryId = 2, CostPrice = 2.0m, RetailPrice = 3.5m, WholesalePrice = 2.8m, StockQuantity = 80, MinStockLevel = 15, Unit = "Bottle", IsActive = true },
            new() { Name = "USB Cable 1m", NameAr = "كابل USB 1 م", Barcode = "300001", SKU = "USB001", CategoryId = 3, CostPrice = 1.0m, RetailPrice = 3.0m, WholesalePrice = 2.0m, StockQuantity = 5, MinStockLevel = 20, Unit = "Piece", IsActive = true },
        };
        context.Products.AddRange(products);

        var customer = new Customer
        {
            Name = "Walk-in Customer",
            NameAr = "زبون عادي",
            Phone = "0500000000",
            CreditLimit = 1000,
            Balance = 0
        };
        context.Customers.Add(customer);

        var supplier = new Supplier
        {
            Name = "Main Supplier",
            NameAr = "المورد الرئيسي",
            Phone = "0511111111",
            Balance = 0
        };
        context.Suppliers.Add(supplier);

        var settings = new List<Setting>
        {
            new() { Key = "company_name", Value = "Ketaba POS", Group = "Company" },
            new() { Key = "company_name_ar", Value = "كتابة نقاط البيع", Group = "Company" },
            new() { Key = "tax_rate", Value = "0", Group = "Tax" },
            new() { Key = "currency", Value = "SAR", Group = "Company" },
            new() { Key = "language", Value = "en", Group = "Localization" },
            new() { Key = "theme", Value = "Light", Group = "Appearance" },
            new() { Key = "receipt_width", Value = "80", Group = "Receipt" },
        };
        context.Settings.AddRange(settings);

        await context.SaveChangesAsync();
    }
}
