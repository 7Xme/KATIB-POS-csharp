using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class LoansViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    [ObservableProperty]
    private ObservableCollection<Loan> _loans = new();

    [ObservableProperty]
    private LoanType _selectedLoanType;

    [ObservableProperty]
    private bool _isLoading;

    public LoansViewModel(AppDbContext context)
    {
        _context = context;
    }

    [RelayCommand]
    private async Task LoadLoansAsync()
    {
        IsLoading = true;
        try
        {
            var query = _context.Loans
                .Include(l => l.Customer)
                .Include(l => l.Supplier)
                .Where(l => l.LoanType == SelectedLoanType)
                .OrderByDescending(l => l.CreatedAt);

            var loans = await query.ToListAsync();
            Loans = new ObservableCollection<Loan>(loans);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task MarkAsPaidAsync(Loan loan)
    {
        loan.Status = LoanStatus.Paid;
        loan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await LoadLoansAsync();
    }
}
