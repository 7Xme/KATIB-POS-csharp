using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class SuppliersViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public SuppliersViewModel(AppDbContext context)
    {
        _context = context;
    }

    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        IsLoading = true;
        try
        {
            var query = _context.Suppliers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(s => s.Name.Contains(SearchText) || s.Phone!.Contains(SearchText));

            var suppliers = await query.OrderBy(s => s.Name).ToListAsync();
            Suppliers = new ObservableCollection<Supplier>(suppliers);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
