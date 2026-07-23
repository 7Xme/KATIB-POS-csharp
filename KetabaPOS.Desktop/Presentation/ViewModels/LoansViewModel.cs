using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Enums;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class LoansViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    [ObservableProperty] private ObservableCollection<Loan> _loans = new();
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private ObservableCollection<Supplier> _suppliers = new();
    [ObservableProperty] private LoanType _selectedLoanType;
    [ObservableProperty] private bool _isLoading;
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
        try
        {
            Loans = new ObservableCollection<Loan>(await _context.Loans
                .Include(l => l.Customer).Include(l => l.Supplier)
                .Where(l => l.LoanType == SelectedLoanType)
                .OrderByDescending(l => l.CreatedAt).ToListAsync());
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        Customers = new ObservableCollection<Customer>(
            await _context.Customers.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync());
    }

    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        Suppliers = new ObservableCollection<Supplier>(
            await _context.Suppliers.Where(s => !s.IsDeleted).OrderBy(s => s.Name).ToListAsync());
    }

    partial void OnSelectedLoanTypeChanged(LoanType value)
    {
        _ = LoadLoansAsync();
    }

    [RelayCommand]
    private void ShowAddForm()
    {
        ResetForm();
        ShowForm = true;
        FormTitle = "Add Loan";
    }

    [RelayCommand]
    private void CancelForm()
    {
        ShowForm = false;
        ResetForm();
    }

    [RelayCommand]
    private async Task SaveLoanAsync()
    {
        if (FormAmount <= 0)
        {
            MessageBox.Show("Loan amount must be greater than zero.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (FormId == 0)
        {
            var loan = new Loan
            {
                LoanType = SelectedLoanType,
                CustomerId = SelectedLoanType == LoanType.CustomerLoan ? FormCustomerId : null,
                SupplierId = SelectedLoanType == LoanType.SupplierLoan ? FormSupplierId : null,
                Amount = FormAmount,
                PaidAmount = FormPaidAmount,
                InterestRate = FormInterestRate,
                DueDate = FormDueDate,
                Notes = FormNotes,
                Status = FormAmount > FormPaidAmount ? LoanStatus.Active : LoanStatus.Paid
            };
            _context.Loans.Add(loan);
        }
        else
        {
            var loan = await _context.Loans.FindAsync(FormId);
            if (loan == null) return;
            loan.Amount = FormAmount;
            loan.PaidAmount = FormPaidAmount;
            loan.InterestRate = FormInterestRate;
            loan.DueDate = FormDueDate;
            loan.Notes = FormNotes;
            loan.CustomerId = SelectedLoanType == LoanType.CustomerLoan ? FormCustomerId : null;
            loan.SupplierId = SelectedLoanType == LoanType.SupplierLoan ? FormSupplierId : null;
            loan.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        ShowForm = false;
        ResetForm();
        await LoadLoansAsync();
    }

    [RelayCommand]
    private async Task ShowLoanPaymentsAsync(Loan loan)
    {
        SelectedLoan = loan;
        LoanPayments = new ObservableCollection<LoanPayment>(
            await _context.LoanPayments.Where(p => p.LoanId == loan.Id)
                .OrderByDescending(p => p.PaymentDate).ToListAsync());
        ShowPayments = true;
        PaymentAmount = 0;
    }

    [RelayCommand]
    private void ClosePayments()
    {
        ShowPayments = false;
        SelectedLoan = null;
        LoanPayments.Clear();
    }

    [RelayCommand]
    private async Task AddPaymentAsync()
    {
        if (SelectedLoan == null || PaymentAmount <= 0) return;

        var payment = new LoanPayment
        {
            LoanId = SelectedLoan.Id,
            Amount = PaymentAmount,
            PaymentDate = DateTime.UtcNow
        };

        SelectedLoan.PaidAmount += PaymentAmount;
        if (SelectedLoan.PaidAmount >= SelectedLoan.Amount)
            SelectedLoan.Status = LoanStatus.Paid;

        SelectedLoan.UpdatedAt = DateTime.UtcNow;
        _context.LoanPayments.Add(payment);
        await _context.SaveChangesAsync();
        await ShowLoanPaymentsAsync(SelectedLoan);
        await LoadLoansAsync();
    }

    [RelayCommand]
    private async Task MarkAsPaidAsync(Loan loan)
    {
        var remaining = loan.Amount - loan.PaidAmount;
        if (remaining > 0)
        {
            _context.LoanPayments.Add(new LoanPayment
            {
                LoanId = loan.Id,
                Amount = remaining,
                PaymentDate = DateTime.UtcNow,
                Notes = "Marked as paid"
            });
        }
        loan.Status = LoanStatus.Paid;
        loan.PaidAmount = loan.Amount;
        loan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await LoadLoansAsync();
    }

    [RelayCommand]
    private async Task DeleteLoanAsync(Loan loan)
    {
        var result = MessageBox.Show($"Delete this loan (Amount: {loan.Amount:N2})?", "Confirm Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            loan.IsDeleted = true;
            loan.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await LoadLoansAsync();
        }
    }

    private void ResetForm()
    {
        FormId = 0;
        FormCustomerId = null;
        FormSupplierId = null;
        FormAmount = 0;
        FormPaidAmount = 0;
        FormInterestRate = 0;
        FormDueDate = DateTime.Today.AddDays(30);
        FormNotes = string.Empty;
    }
}
