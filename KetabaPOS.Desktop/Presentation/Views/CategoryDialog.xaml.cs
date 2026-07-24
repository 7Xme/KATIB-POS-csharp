using System.Windows;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KetabaPOS.Desktop.Presentation.Views;

public partial class CategoryDialog : Window
{
    private readonly AppDbContext _context;

    public CategoryDialog(AppDbContext context)
    {
        InitializeComponent();
        _context = context;
        Loaded += async (s, e) => await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        CategoryList.ItemsSource = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    private async void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        var name = CategoryNameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter a category name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var exists = await _context.Categories.AnyAsync(c => c.Name == name);
        if (exists)
        {
            MessageBox.Show("Category already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _context.Categories.Add(new Category { Name = name });
        await _context.SaveChangesAsync();
        CategoryNameBox.Text = string.Empty;
        await LoadCategoriesAsync();
    }

    private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        var category = (sender as FrameworkElement)?.DataContext as Category;
        if (category == null) return;

        var productCount = await _context.Products.CountAsync(p => p.CategoryId == category.Id);
        if (productCount > 0)
        {
            MessageBox.Show($"Cannot delete — {productCount} product(s) use this category.", "In Use",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show($"Delete \"{category.Name}\"?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        await LoadCategoriesAsync();
    }
}
