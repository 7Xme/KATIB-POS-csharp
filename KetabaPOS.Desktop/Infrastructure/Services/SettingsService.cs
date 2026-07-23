using System.IO;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace KetabaPOS.Desktop.Infrastructure.Services;
public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;
    private readonly string _dbPath;
    public SettingsService(AppDbContext context) { _context = context; _dbPath = context.Database.GetConnectionString() ?? ""; }
    public async Task<string?> GetSettingAsync(string key) { var s = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key); return s?.Value; }
    public async Task SetSettingAsync(string key, string value, string? group = null)
    {
        var s = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (s == null) _context.Settings.Add(new Setting { Key = key, Value = value, Group = group });
        else { s.Value = value; s.UpdatedAt = DateTime.UtcNow; }
        await _context.SaveChangesAsync();
    }
    public async Task<Dictionary<string, string>> GetSettingsByGroupAsync(string group) => await _context.Settings.Where(s => s.Group == group).ToDictionaryAsync(s => s.Key, s => s.Value);
    public async Task<bool> BackupDatabaseAsync(string filePath)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath); if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(_dbPath)) { File.Copy(_dbPath, filePath, true); _context.BackupLogs.Add(new BackupLog { FileName = Path.GetFileName(filePath), FilePath = filePath, FileSize = new FileInfo(filePath).Length, IsSuccess = true, CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync(); return true; }
            return false;
        }
        catch (Exception ex) { _context.BackupLogs.Add(new BackupLog { FileName = Path.GetFileName(filePath), FilePath = filePath, IsSuccess = false, ErrorMessage = ex.Message, CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync(); return false; }
    }
    public async Task<bool> RestoreDatabaseAsync(string filePath) { try { if (!File.Exists(filePath)) return false; await _context.Database.CloseConnectionAsync(); File.Copy(filePath, _dbPath, true); return true; } catch { return false; } }
}
