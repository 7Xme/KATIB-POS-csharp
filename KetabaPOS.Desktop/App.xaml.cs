using System.Globalization;
using System.IO;
using System.Threading;
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
    private static Mutex? _instanceMutex;

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
        var logPath = Path.Combine(Path.GetTempPath(), "KetabaPOS_startup.log");
        try
        {
            File.WriteAllText(logPath, $"Starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

            // Prevent multiple instances using a named Mutex
            _instanceMutex = new Mutex(true, "KetabaPOS_InstanceMutex", out var createdNew);
            if (!createdNew)
            {
                var msg = "Ketaba POS is already running. Only one instance is allowed.";
                File.AppendAllText(logPath, $"WARN: {msg}\n");
                MessageBox.Show(msg, "Already Running", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            var dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KetabaPOS");
            File.AppendAllText(logPath, $"DB folder: {dbFolder}\n");
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
            services.AddSingleton<IReceiptService, ReceiptService>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<PosViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<CustomersViewModel>();
            services.AddTransient<SuppliersViewModel>();
            services.AddTransient<SalesViewModel>();
            services.AddTransient<LoansViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<PurchasesViewModel>();
            services.AddTransient<SettingsViewModel>();

            _serviceProvider = services.BuildServiceProvider();
            File.AppendAllText(logPath, "ServiceProvider built\n");

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Database.EnsureCreated();
                File.AppendAllText(logPath, "Database ensured\n");
                DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
                File.AppendAllText(logPath, "Database seeded\n");

                var langSetting = context.Settings.FirstOrDefault(s => s.Key == "language");
                if (langSetting != null && langSetting.Value == "ar")
                    TranslationSource.Instance.SwitchTo("ar");
                File.AppendAllText(logPath, $"Culture set to: {CultureInfo.CurrentUICulture.Name}\n");
            }

            var mainWindow = new MainWindow(_serviceProvider.GetRequiredService<MainViewModel>());
            File.AppendAllText(logPath, "MainWindow created\n");
            mainWindow.Show();
            File.AppendAllText(logPath, "MainWindow shown\n");

            base.OnStartup(e);
        }
        catch (System.Exception ex)
        {
            var msg = $"Application failed to start:\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            File.AppendAllText(logPath, $"ERROR: {msg}\n");
            MessageBox.Show(msg, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
