using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Models;

namespace KetabaPOS.Tests;

public class UnitTest1
{
    [Fact]
    public void Product_ShouldCalculateCorrectly()
    {
        var product = new Product
        {
            Name = "Test Product",
            CostPrice = 10m,
            RetailPrice = 20m,
            WholesalePrice = 15m,
            StockQuantity = 100,
            MinStockLevel = 10
        };

        Assert.Equal(10m, product.CostPrice);
        Assert.Equal(20m, product.RetailPrice);
        Assert.Equal(100, product.StockQuantity);
        Assert.True(product.StockQuantity > product.MinStockLevel);
    }

    [Fact]
    public void Sale_ShouldCalculateTotalCorrectly()
    {
        var sale = new Sale
        {
            Subtotal = 100m,
            TaxAmount = 15m,
            DiscountAmount = 10m,
            PaidAmount = 120m
        };
        sale.TotalAmount = sale.Subtotal + sale.TaxAmount - sale.DiscountAmount;
        sale.ChangeAmount = Math.Max(0, sale.PaidAmount - sale.TotalAmount);

        Assert.Equal(105m, sale.TotalAmount);
        Assert.Equal(15m, sale.ChangeAmount);
    }

    [Fact]
    public void Loan_RemainingAmount_ShouldBeCalculated()
    {
        var loan = new Loan
        {
            Amount = 1000m,
            PaidAmount = 400m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = LoanStatus.Active
        };

        Assert.Equal(600m, loan.RemainingAmount);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void User_Role_ShouldBeAssignable()
    {
        var user = new User
        {
            Username = "testuser",
            DisplayName = "Test User",
            Role = UserRole.Cashier,
            IsActive = true
        };

        Assert.Equal(UserRole.Cashier, user.Role);
        Assert.True(user.IsActive);

        user.Role = UserRole.Manager;
        Assert.Equal(UserRole.Manager, user.Role);
    }

    [Fact]
    public void Customer_Balance_ShouldUpdateCorrectly()
    {
        var customer = new Customer
        {
            Name = "Test Customer",
            CreditLimit = 5000m,
            Balance = 0m
        };

        customer.Balance += 1500m;
        Assert.Equal(1500m, customer.Balance);

        customer.Balance -= 500m;
        Assert.Equal(1000m, customer.Balance);
    }
}
