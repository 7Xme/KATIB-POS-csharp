using System.Windows;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Infrastructure.Data;
using KetabaPOS.Desktop.Infrastructure.Services;
using KetabaPOS.Desktop.Presentation.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KetabaPOS.Desktop;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var dbFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KetabaPOS");
                System.IO.Directory.CreateDirectory(dbFolder);
                var dbPath = System.IO.Path.Combine(dbFolder, "ketaba.db");

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                services.AddScoped<IAuthService, AuthService>();
                services.AddScoped<IProductService, ProductService>();
                services.AddScoped<ISaleService, SaleService>();
                services.AddScoped<IDashboardService, DashboardService>();
                services.AddScoped<ISettingsService, SettingsService>();

                services.AddTransient<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<PosViewModel>();
                services.AddTransient<ProductsViewModel>();
                services.AddTransient<CustomersViewModel>();
                services.AddTransient<SuppliersViewModel>();
                services.AddTransient<SalesViewModel>();
                services.AddTransient<LoansViewModel>();
                services.AddTransient<SettingsViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        using (var scope = _host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
            await DbSeeder.SeedAsync(context);
        }

        var mainWindow = new MainWindow(_host.Services.GetRequiredService<MainViewModel>());
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host.Dispose();
        base.OnExit(e);
    }
}
