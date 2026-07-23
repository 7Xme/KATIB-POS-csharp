using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Models;
using Microsoft.EntityFrameworkCore;
namespace KetabaPOS.Desktop.Infrastructure.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<BackupLog> BackupLogs => Set<BackupLog>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(e => { e.HasIndex(u => u.Username).IsUnique(); e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20); e.HasQueryFilter(u => !u.IsDeleted); });
        modelBuilder.Entity<Product>(e => { e.HasIndex(p => p.Barcode); e.HasIndex(p => p.SKU); e.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId); e.Property(p => p.UnitPrice).HasPrecision(18, 2); e.Property(p => p.CostPrice).HasPrecision(18, 2); e.Property(p => p.RetailPrice).HasPrecision(18, 2); e.Property(p => p.WholesalePrice).HasPrecision(18, 2); e.HasQueryFilter(p => !p.IsDeleted); });
        modelBuilder.Entity<Category>(e => { e.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict); e.HasQueryFilter(c => !c.IsDeleted); });
        modelBuilder.Entity<Customer>(e => { e.Property(c => c.Balance).HasPrecision(18, 2); e.Property(c => c.CreditLimit).HasPrecision(18, 2); e.HasQueryFilter(c => !c.IsDeleted); });
        modelBuilder.Entity<Supplier>(e => { e.Property(s => s.Balance).HasPrecision(18, 2); e.HasQueryFilter(s => !s.IsDeleted); });
        modelBuilder.Entity<Sale>(e => { e.Property(s => s.Subtotal).HasPrecision(18, 2); e.Property(s => s.TaxAmount).HasPrecision(18, 2); e.Property(s => s.DiscountAmount).HasPrecision(18, 2); e.Property(s => s.TotalAmount).HasPrecision(18, 2); e.Property(s => s.PaidAmount).HasPrecision(18, 2); e.Property(s => s.ChangeAmount).HasPrecision(18, 2); e.Property(s => s.PaymentMethod).HasConversion<string>().HasMaxLength(20); e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20); e.HasOne(s => s.Customer).WithMany(c => c.Sales).HasForeignKey(s => s.CustomerId); e.HasOne(s => s.User).WithMany(u => u.Sales).HasForeignKey(s => s.UserId); });
        modelBuilder.Entity<SaleItem>(e => { e.Property(si => si.UnitPrice).HasPrecision(18, 2); e.Property(si => si.DiscountAmount).HasPrecision(18, 2); e.Property(si => si.TotalPrice).HasPrecision(18, 2); e.HasOne(si => si.Sale).WithMany(s => s.SaleItems).HasForeignKey(si => si.SaleId); e.HasOne(si => si.Product).WithMany(p => p.SaleItems).HasForeignKey(si => si.ProductId); });
        modelBuilder.Entity<Purchase>(e => { e.Property(p => p.Subtotal).HasPrecision(18, 2); e.Property(p => p.TaxAmount).HasPrecision(18, 2); e.Property(p => p.TotalAmount).HasPrecision(18, 2); e.Property(p => p.PaidAmount).HasPrecision(18, 2); e.HasOne(p => p.Supplier).WithMany(s => s.Purchases).HasForeignKey(p => p.SupplierId); });
        modelBuilder.Entity<PurchaseItem>(e => { e.Property(pi => pi.UnitCost).HasPrecision(18, 2); e.Property(pi => pi.TotalCost).HasPrecision(18, 2); e.HasOne(pi => pi.Purchase).WithMany(p => p.PurchaseItems).HasForeignKey(pi => pi.PurchaseId); e.HasOne(pi => pi.Product).WithMany(p => p.PurchaseItems).HasForeignKey(pi => pi.ProductId); });
        modelBuilder.Entity<InventoryTransaction>(e => { e.Property(it => it.Type).HasConversion<string>().HasMaxLength(20); e.HasOne(it => it.Product).WithMany(p => p.InventoryTransactions).HasForeignKey(it => it.ProductId); });
        modelBuilder.Entity<Loan>(e => { e.Property(l => l.Amount).HasPrecision(18, 2); e.Property(l => l.PaidAmount).HasPrecision(18, 2); e.Property(l => l.InterestRate).HasPrecision(5, 2); e.Property(l => l.LoanType).HasConversion<string>().HasMaxLength(20); e.Property(l => l.Status).HasConversion<string>().HasMaxLength(20); e.HasOne(l => l.Customer).WithMany(c => c.Loans).HasForeignKey(l => l.CustomerId); e.HasOne(l => l.Supplier).WithMany(s => s.Loans).HasForeignKey(l => l.SupplierId); });
        modelBuilder.Entity<LoanPayment>(e => { e.Property(lp => lp.Amount).HasPrecision(18, 2); e.HasOne(lp => lp.Loan).WithMany(l => l.LoanPayments).HasForeignKey(lp => lp.LoanId); });
        modelBuilder.Entity<Setting>(e => { e.HasIndex(s => s.Key).IsUnique(); });
        modelBuilder.Entity<BackupLog>(e => { e.HasQueryFilter(bl => !bl.IsDeleted); });
    }
}
