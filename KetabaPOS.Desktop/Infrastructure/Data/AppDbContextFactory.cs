using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace KetabaPOS.Desktop.Infrastructure.Data;
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KetabaPOS");
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseSqlite($"Data Source={Path.Combine(dbFolder, "ketaba.db")}");
        return new AppDbContext(builder.Options);
    }
}
