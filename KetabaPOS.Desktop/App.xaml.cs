using System.IO;
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
        DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show($"Runtime error:\n{e.Exception.GetType().Name}: {e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            MessageBox.Show($"Background task error:\n{e.Exception?.GetType().Name}: {e.Exception?.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.SetObserved();
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            var dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KetabaPOS");
            Directory.CreateDirectory(dbFolder);
            var dbPath = Path.Combine(dbFolder, "ketaba.db");
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
            services.AddSingleton<IPurchaseService, PurchaseService>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<PosViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<CustomersViewModel>();
            services.AddTransient<SuppliersViewModel>();
            services.AddTransient<SalesViewModel>();
            services.AddTransient<LoansViewModel>();
            services.AddTransient<PurchasesViewModel>();
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
            MessageBox.Show($"Application failed to start:\n{ex.GetType().Name}: {ex.Message}",
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
