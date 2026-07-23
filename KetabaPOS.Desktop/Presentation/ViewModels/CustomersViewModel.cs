using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class CustomersViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public CustomersViewModel(AppDbContext context)
    {
        _context = context;
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        IsLoading = true;
        try
        {
            var query = _context.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(c => c.Name.Contains(SearchText) || c.Phone!.Contains(SearchText));

            var customers = await query.OrderBy(c => c.Name).ToListAsync();
            Customers = new ObservableCollection<Customer>(customers);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
