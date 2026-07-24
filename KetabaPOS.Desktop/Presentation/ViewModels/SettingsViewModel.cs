using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using Microsoft.Win32;
using System.Windows;

namespace KetabaPOS.Desktop.Presentation.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _companyNameAr = string.Empty;
    [ObservableProperty] private string _taxRate = "0";
    [ObservableProperty] private string _currency = "SAR";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;

    public SettingsViewModel(ISettingsService settingsService) { _settingsService = settingsService; }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            CompanyName = await _settingsService.GetSettingAsync("company_name") ?? "";
            CompanyNameAr = await _settingsService.GetSettingAsync("company_name_ar") ?? "";
            TaxRate = await _settingsService.GetSettingAsync("tax_rate") ?? "0";
            Currency = await _settingsService.GetSettingAsync("currency") ?? "SAR";
        }
        catch (Exception ex) { StatusMessage = $"Error loading settings: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await _settingsService.SetSettingAsync("company_name", CompanyName, "Company");
            await _settingsService.SetSettingAsync("company_name_ar", CompanyNameAr, "Company");
            await _settingsService.SetSettingAsync("tax_rate", TaxRate, "Tax");
            await _settingsService.SetSettingAsync("currency", Currency, "Company");
            StatusMessage = "Settings saved successfully!";
        }
        catch (Exception ex) { StatusMessage = $"Error saving settings: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        var d = new SaveFileDialog
        {
            Filter = "Database files (*.db)|*.db|All files (*.*)|*.*",
            DefaultExt = ".db",
            FileName = $"ketaba_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
        };
        if (d.ShowDialog() != true) return;
        IsLoading = true;
        try
        {
            StatusMessage = (await _settingsService.BackupDatabaseAsync(d.FileName))
                ? "Backup created successfully!"
                : "Backup failed.";
        }
        catch (Exception ex) { StatusMessage = $"Backup error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        var d = new OpenFileDialog { Filter = "Database files (*.db)|*.db|All files (*.*)|*.*", DefaultExt = ".db" };
        if (d.ShowDialog() != true) return;
        var result = MessageBox.Show(
            "Restore will overwrite all current data and restart the application. Continue?",
            "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        IsLoading = true;
        try
        {
            var success = await _settingsService.RestoreDatabaseAsync(d.FileName);
            if (success)
            {
                MessageBox.Show("Database restored. The application will now restart.", "Restore Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                System.Windows.Application.Current.Shutdown();
            }
            else { StatusMessage = "Restore failed."; }
        }
        catch (Exception ex) { StatusMessage = $"Restore error: {ex.Message}"; }
        finally { IsLoading = false; }
    }
}
