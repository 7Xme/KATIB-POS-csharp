using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KetabaPOS.Desktop.Core.Interfaces;
using Microsoft.Win32;
namespace KetabaPOS.Desktop.Presentation.ViewModels;
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _companyNameAr = string.Empty;
    [ObservableProperty] private string _taxRate = "0";
    [ObservableProperty] private string _currency = "SAR";
    [ObservableProperty] private string _language = "en";
    [ObservableProperty] private string _theme = "Light";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;
    public SettingsViewModel(ISettingsService settingsService) { _settingsService = settingsService; }
    [RelayCommand] private async Task LoadSettingsAsync() { IsLoading = true; try { CompanyName = await _settingsService.GetSettingAsync("company_name") ?? ""; CompanyNameAr = await _settingsService.GetSettingAsync("company_name_ar") ?? ""; TaxRate = await _settingsService.GetSettingAsync("tax_rate") ?? "0"; Currency = await _settingsService.GetSettingAsync("currency") ?? "SAR"; Language = await _settingsService.GetSettingAsync("language") ?? "en"; Theme = await _settingsService.GetSettingAsync("theme") ?? "Light"; } finally { IsLoading = false; } }
    [RelayCommand] private async Task SaveSettingsAsync() { IsLoading = true; try { await _settingsService.SetSettingAsync("company_name", CompanyName, "Company"); await _settingsService.SetSettingAsync("company_name_ar", CompanyNameAr, "Company"); await _settingsService.SetSettingAsync("tax_rate", TaxRate, "Tax"); await _settingsService.SetSettingAsync("currency", Currency, "Company"); await _settingsService.SetSettingAsync("language", Language, "Localization"); await _settingsService.SetSettingAsync("theme", Theme, "Appearance"); StatusMessage = "Settings saved successfully!"; } catch { StatusMessage = "Error saving settings."; } finally { IsLoading = false; } }
    [RelayCommand] private async Task BackupDatabaseAsync() { var d = new SaveFileDialog { Filter = "Database files (*.db)|*.db|All files (*.*)|*.*", DefaultExt = ".db", FileName = $"ketaba_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db" }; if (d.ShowDialog() == true) StatusMessage = (await _settingsService.BackupDatabaseAsync(d.FileName)) ? "Backup created successfully!" : "Backup failed."; }
    [RelayCommand] private async Task RestoreDatabaseAsync() { var d = new OpenFileDialog { Filter = "Database files (*.db)|*.db|All files (*.*)|*.*", DefaultExt = ".db" }; if (d.ShowDialog() == true) StatusMessage = (await _settingsService.RestoreDatabaseAsync(d.FileName)) ? "Database restored successfully!" : "Restore failed."; }
}
