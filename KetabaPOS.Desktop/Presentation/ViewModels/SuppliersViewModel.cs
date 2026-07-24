using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class SuppliersViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _showForm;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _formTitle = "Add Supplier";
    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formNameAr = string.Empty;
    [ObservableProperty] private string _formPhone = string.Empty;
    [ObservableProperty] private string _formEmail = string.Empty;
    [ObservableProperty] private string _formAddress = string.Empty;

    public SuppliersViewModel(AppDbContext context) { _context = context; }

    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var q = _context.Suppliers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
                q = q.Where(s => s.Name.Contains(SearchText) || (s.Phone != null && s.Phone.Contains(SearchText)));
            Suppliers = new ObservableCollection<Supplier>(await q.OrderBy(s => s.Name).ToListAsync());
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadSuppliersAsync();

    [RelayCommand]
    private void ShowAddForm() { ResetForm(); ShowForm = true; IsEditing = false; FormTitle = "Add Supplier"; }

    [RelayCommand]
    private void ShowEditForm(Supplier supplier)
    {
        if (supplier == null) return;
        FormId = supplier.Id; FormName = supplier.Name; FormNameAr = supplier.NameAr;
        FormPhone = supplier.Phone ?? string.Empty; FormEmail = supplier.Email ?? string.Empty;
        FormAddress = supplier.Address ?? string.Empty;
        ShowForm = true; IsEditing = true; FormTitle = "Edit Supplier";
    }

    [RelayCommand]
    private void CancelForm() { ShowForm = false; ResetForm(); }

    [RelayCommand]
    private async Task SaveSupplierAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            MessageBox.Show("Supplier name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        IsLoading = true;
        try
        {
            if (IsEditing)
            {
                var s = await _context.Suppliers.FindAsync(FormId);
                if (s == null) { StatusMessage = "Supplier not found."; return; }
                s.Name = FormName; s.NameAr = FormNameAr; s.Phone = FormPhone; s.Email = FormEmail;
                s.Address = FormAddress; s.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.Suppliers.Add(new Supplier
                {
                    Name = FormName, NameAr = FormNameAr, Phone = FormPhone,
                    Email = FormEmail, Address = FormAddress
                });
            }
            await _context.SaveChangesAsync();
            ShowForm = false; ResetForm(); StatusMessage = "Supplier saved.";
            await LoadSuppliersAsync();
        }
        catch (Exception ex) { StatusMessage = $"Save failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task DeleteSupplierAsync(Supplier supplier)
    {
        if (supplier == null) return;
        if (MessageBox.Show($"Delete \"{supplier.Name}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        IsLoading = true;
        try
        {
            supplier.IsDeleted = true; supplier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            StatusMessage = "Supplier deleted.";
            await LoadSuppliersAsync();
        }
        catch (Exception ex) { StatusMessage = $"Delete failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private void ResetForm()
    {
        FormId = 0; FormName = string.Empty; FormNameAr = string.Empty;
        FormPhone = string.Empty; FormEmail = string.Empty; FormAddress = string.Empty;
    }
}
