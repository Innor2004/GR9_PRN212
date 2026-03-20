using System;
using System.IO;
using System.Windows;
using EWMS_WPF.BLL;
using EWMS_WPF.DAL;
using EWMS_WPF.Models;
using EWMS_WPF.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EWMS_WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        Configuration = builder.Build();

        // Setup Dependency Injection
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        ServiceProvider = serviceCollection.BuildServiceProvider();

        // Show Login window
        var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        services.AddSingleton(Configuration);

        // DbContext
        services.AddDbContext<EWMSDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DBContext")));

        // DAL
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // BLL - Session
        services.AddSingleton<SessionService>();

        // BLL - ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<PurchaseOrderViewModel>();
        services.AddTransient<PurchaseOrderCreateViewModel>();
        services.AddTransient<SalesOrderViewModel>();
        services.AddTransient<SalesOrderCreateViewModel>();
        services.AddTransient<StockInViewModel>();
        services.AddTransient<StockOutViewModel>();
        services.AddTransient<InventoryViewModel>();

        // Views
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindowPurchase>();
        services.AddTransient<MainWindowSales>();
        services.AddTransient<MainWindowInventory>();
        services.AddTransient<Views.PurchaseOrder.PurchaseOrderListView>();
        services.AddTransient<Views.PurchaseOrder.PurchaseOrderCreateView>();
        services.AddTransient<Views.PurchaseOrder.PurchaseOrderDetailsView>();
        services.AddTransient<Views.SalesOrder.SalesOrderListView>();
        services.AddTransient<Views.SalesOrder.SalesOrderCreateView>();
        services.AddTransient<Views.SalesOrder.SalesOrderDetailsView>();
        services.AddTransient<Views.StockIn.StockInListView>();
        services.AddTransient<Views.StockOut.StockOutListView>();
        services.AddTransient<Views.Inventory.InventoryView>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}

