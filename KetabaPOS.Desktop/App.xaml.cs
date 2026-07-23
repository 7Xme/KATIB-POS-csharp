using System.Windows;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Infrastructure.Data;
using KetabaPOS.Desktop.Infrastructure.Services;
using KetabaPOS.Desktop.Presentation.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KetabaPOS.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public App()
    {
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            var dbFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KetabaPOS");
            System.IO.Directory.CreateDirectory(dbFolder);
            var dbPath = System.IO.Path.Combine(dbFolder, "ketaba.db");
            var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();

            var services = new ServiceCollection();

            services.AddSingleton<AppDbContext>(sp =>
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connectionString)
                    .Options;
                return new AppDbContext(options);
            });

            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<ISaleService, SaleService>();
            services.AddSingleton<IDashboardService, DashboardService>();
            services.AddSingleton<ISettingsService, SettingsService>();

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

            _serviceProvider = services.BuildServiceProvider();

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Database.EnsureCreated();
                DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
            }

            var mainWindow = new MainWindow(_serviceProvider.GetRequiredService<MainViewModel>());
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Application failed to start:\n{ex.GetType().Name}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
