using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class CustomersViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _showForm;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _formTitle = "Add Customer";

    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formNameAr = string.Empty;
    [ObservableProperty] private string _formPhone = string.Empty;
    [ObservableProperty] private string _formEmail = string.Empty;
    [ObservableProperty] private string _formAddress = string.Empty;
    [ObservableProperty] private decimal _formCreditLimit;

    public CustomersViewModel(AppDbContext context) { _context = context; }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        IsLoading = true;
        try
        {
            var q = _context.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
                q = q.Where(c => c.Name.Contains(SearchText) || (c.Phone != null && c.Phone.Contains(SearchText)));
            Customers = new ObservableCollection<Customer>(await q.OrderBy(c => c.Name).ToListAsync());
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private void ShowAddForm()
    {
        ResetForm();
        ShowForm = true;
        IsEditing = false;
        FormTitle = "Add Customer";
    }

    [RelayCommand]
    private void ShowEditForm(Customer customer)
    {
        FormId = customer.Id;
        FormName = customer.Name;
        FormNameAr = customer.NameAr;
        FormPhone = customer.Phone ?? string.Empty;
        FormEmail = customer.Email ?? string.Empty;
        FormAddress = customer.Address ?? string.Empty;
        FormCreditLimit = customer.CreditLimit;
        ShowForm = true;
        IsEditing = true;
        FormTitle = "Edit Customer";
    }

    [RelayCommand]
    private void CancelForm()
    {
        ShowForm = false;
        ResetForm();
    }

    [RelayCommand]
    private async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            MessageBox.Show("Customer name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IsEditing)
        {
            var c = await _context.Customers.FindAsync(FormId);
            if (c == null) return;
            c.Name = FormName;
            c.NameAr = FormNameAr;
            c.Phone = FormPhone;
            c.Email = FormEmail;
            c.Address = FormAddress;
            c.CreditLimit = FormCreditLimit;
            c.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var c = new Customer
            {
                Name = FormName,
                NameAr = FormNameAr,
                Phone = FormPhone,
                Email = FormEmail,
                Address = FormAddress,
                CreditLimit = FormCreditLimit
            };
            _context.Customers.Add(c);
        }

        await _context.SaveChangesAsync();
        ShowForm = false;
        ResetForm();
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync(Customer customer)
    {
        var result = MessageBox.Show($"Delete customer \"{customer.Name}\"?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            customer.IsDeleted = true;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await LoadCustomersAsync();
        }
    }

    private void ResetForm()
    {
        FormId = 0;
        FormName = string.Empty;
        FormNameAr = string.Empty;
        FormPhone = string.Empty;
        FormEmail = string.Empty;
        FormAddress = string.Empty;
        FormCreditLimit = 0;
    }
}
