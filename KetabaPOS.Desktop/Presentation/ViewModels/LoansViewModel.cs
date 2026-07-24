using System.Collections.ObjectModel;
using System.Windows;
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

    [ObservableProperty] private ObservableCollection<Loan> _loans = new();
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();
    [ObservableProperty] private LoanType _selectedLoanType;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _showForm;
    [ObservableProperty] private string _formTitle = "Add Loan";
    [ObservableProperty] private int _formId;
    [ObservableProperty] private int? _formCustomerId;
    [ObservableProperty] private int? _formSupplierId;
    [ObservableProperty] private decimal _formAmount;
    [ObservableProperty] private decimal _formPaidAmount;
    [ObservableProperty] private decimal _formInterestRate;
    [ObservableProperty] private DateTime _formDueDate = DateTime.Today.AddDays(30);
    [ObservableProperty] private string _formNotes = string.Empty;
    [ObservableProperty] private Loan? _selectedLoan;
    [ObservableProperty] private ObservableCollection<LoanPayment> _loanPayments = new();
    [ObservableProperty] private bool _showPayments;
    [ObservableProperty] private decimal _paymentAmount;

    public LoansViewModel(AppDbContext context) { _context = context; }

    [RelayCommand]
    private async Task LoadLoansAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            Loans = new ObservableCollection<Loan>(await _context.Loans
                .Include(l => l.Customer).Include(l => l.Supplier)
                .Where(l => l.LoanType == SelectedLoanType)
                .OrderByDescending(l => l.CreatedAt).ToListAsync());
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        try
        {
            Customers = new ObservableCollection<Customer>(
                await _context.Customers.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync());
        }
        catch (Exception ex) { StatusMessage = $"Error loading customers: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        try
        {
            Suppliers = new ObservableCollection<Supplier>(
                await _context.Suppliers.Where(s => !s.IsDeleted).OrderBy(s => s.Name).ToListAsync());
        }
        catch (Exception ex) { StatusMessage = $"Error loading suppliers: {ex.Message}"; }
    }

    partial void OnSelectedLoanTypeChanged(LoanType value)
    {
        _ = LoadLoansSafeAsync();
    }

    private async Task LoadLoansSafeAsync()
    {
        IsLoading = true;
        try { await LoadLoansAsync(); }
        catch { /* handled in LoadLoansAsync */ }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void ShowAddForm() { ResetForm(); ShowForm = true; FormTitle = "Add Loan"; }

    [RelayCommand]
    private void CancelForm() { ShowForm = false; ResetForm(); }

    [RelayCommand]
    private async Task SaveLoanAsync()
    {
        if (FormAmount <= 0)
        {
            MessageBox.Show("Amount must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SelectedLoanType == LoanType.CustomerLoan && FormCustomerId == null)
        {
            MessageBox.Show("Select a customer for this loan.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SelectedLoanType == LoanType.SupplierLoan && FormSupplierId == null)
        {
            MessageBox.Show("Select a supplier for this loan.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        IsLoading = true;
        try
        {
            if (FormId == 0)
            {
                _context.Loans.Add(new Loan
                {
                    LoanType = SelectedLoanType,
                    CustomerId = SelectedLoanType == LoanType.CustomerLoan ? FormCustomerId : null,
                    SupplierId = SelectedLoanType == LoanType.SupplierLoan ? FormSupplierId : null,
                    Amount = FormAmount, PaidAmount = FormPaidAmount, InterestRate = FormInterestRate,
                    DueDate = FormDueDate, Notes = FormNotes,
                    Status = FormAmount > FormPaidAmount ? LoanStatus.Active : LoanStatus.Paid
                });
            }
            else
            {
                var loan = await _context.Loans.FindAsync(FormId);
                if (loan == null) { StatusMessage = "Loan not found."; return; }
                loan.Amount = FormAmount; loan.PaidAmount = FormPaidAmount; loan.InterestRate = FormInterestRate;
                loan.DueDate = FormDueDate; loan.Notes = FormNotes;
                loan.CustomerId = SelectedLoanType == LoanType.CustomerLoan ? FormCustomerId : null;
                loan.SupplierId = SelectedLoanType == LoanType.SupplierLoan ? FormSupplierId : null;
                loan.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            ShowForm = false; ResetForm(); StatusMessage = "Loan saved.";
            await LoadLoansAsync();
        }
        catch (Exception ex) { StatusMessage = $"Save failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ShowLoanPaymentsAsync(Loan loan)
    {
        if (loan == null) return;
        IsLoading = true;
        try
        {
            SelectedLoan = loan;
            LoanPayments = new ObservableCollection<LoanPayment>(
                await _context.LoanPayments.Where(p => p.LoanId == loan.Id)
                    .OrderByDescending(p => p.PaymentDate).ToListAsync());
            ShowPayments = true; PaymentAmount = 0;
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void ClosePayments() { ShowPayments = false; SelectedLoan = null; LoanPayments.Clear(); }

    [RelayCommand]
    private async Task AddPaymentAsync()
    {
        if (SelectedLoan == null || PaymentAmount <= 0) return;
        IsLoading = true;
        try
        {
            SelectedLoan.PaidAmount += PaymentAmount;
            SelectedLoan.PaidAmount = System.Math.Min(SelectedLoan.PaidAmount, SelectedLoan.Amount);
            if (SelectedLoan.PaidAmount >= SelectedLoan.Amount)
                SelectedLoan.Status = LoanStatus.Paid;
            SelectedLoan.UpdatedAt = DateTime.UtcNow;
            _context.LoanPayments.Add(new LoanPayment
            {
                LoanId = SelectedLoan.Id, Amount = PaymentAmount, PaymentDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            await ShowLoanPaymentsAsync(SelectedLoan);
            await LoadLoansAsync();
        }
        catch (Exception ex) { StatusMessage = $"Payment failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task MarkAsPaidAsync(Loan loan)
    {
        if (loan == null) return;
        IsLoading = true;
        try
        {
            var remaining = loan.Amount - loan.PaidAmount;
            if (remaining > 0)
            {
                _context.LoanPayments.Add(new LoanPayment { LoanId = loan.Id, Amount = remaining, PaymentDate = DateTime.UtcNow, Notes = "Marked as paid" });
            }
            loan.Status = LoanStatus.Paid; loan.PaidAmount = loan.Amount; loan.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            StatusMessage = "Loan marked as paid.";
            await LoadLoansAsync();
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task DeleteLoanAsync(Loan loan)
    {
        if (loan == null) return;
        if (MessageBox.Show($"Delete this loan ({loan.Amount:N2})?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        IsLoading = true;
        try
        {
            loan.IsDeleted = true; loan.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            StatusMessage = "Loan deleted.";
            await LoadLoansAsync();
        }
        catch (Exception ex) { StatusMessage = $"Delete failed: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private void ResetForm()
    {
        FormId = 0; FormCustomerId = null; FormSupplierId = null; FormAmount = 0;
        FormPaidAmount = 0; FormInterestRate = 0; FormDueDate = DateTime.Today.AddDays(30); FormNotes = string.Empty;
    }
}
