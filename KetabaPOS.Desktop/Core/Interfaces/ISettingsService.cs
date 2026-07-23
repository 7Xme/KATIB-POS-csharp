namespace KetabaPOS.Desktop.Core.Interfaces;
public interface ISettingsService
{
    Task<string?> GetSettingAsync(string key);
    Task SetSettingAsync(string key, string value, string? group = null);
    Task<Dictionary<string, string>> GetSettingsByGroupAsync(string group);
    Task<bool> BackupDatabaseAsync(string filePath);
    Task<bool> RestoreDatabaseAsync(string filePath);
}
