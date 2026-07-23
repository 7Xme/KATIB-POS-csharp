namespace KetabaPOS.Desktop.Core.Models;

public class BackupLog : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
