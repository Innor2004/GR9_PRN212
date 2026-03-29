using System.Windows;
using EWMS_WPF.BLL;
using Microsoft.Extensions.DependencyInjection;

namespace EWMS_WPF.Views
{
    public partial class MainWindowInventory : Window
    {
        private readonly SessionService _sessionService;

        public MainWindowInventory(SessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService;
            LoadUserInfo();
            
            
            Loaded += (s, e) => BtnInventory_Click(s, e);
        }

        private void LoadUserInfo()
        {
            var session = _sessionService.CurrentSession;
            if (session != null)
            {
                txtUserInfo.Text = $"{session.FullName} - {session.WarehouseName}";
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", 
                "Confirm Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _sessionService.ClearSession();
                var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
                loginWindow.Show();
                this.Close();
            }
        }

        private void BtnStockIn_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = App.ServiceProvider.GetRequiredService<StockInViewModel>();
            
            // Setup navigation callbacks
            viewModel.OnNavigateToReceive = async (purchaseOrderId) =>
            {
                await viewModel.LoadPurchaseOrderForReceiveAsync(purchaseOrderId);
                var receiveView = new StockIn.StockInReceiveView(viewModel);
                MainFrame.Navigate(receiveView);
            };
            
            viewModel.OnNavigateBackToList = () =>
            {
                var listView = new StockIn.StockInListView(viewModel);
                MainFrame.Navigate(listView);
            };
            
            var listView = new StockIn.StockInListView(viewModel);
            MainFrame.Navigate(listView);
        }

        private void BtnStockOut_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = App.ServiceProvider.GetRequiredService<StockOutViewModel>();
            
            // Setup navigation callbacks
            viewModel.OnNavigateToIssue = async (salesOrderId) =>
            {
                await viewModel.LoadSalesOrderForIssueAsync(salesOrderId);
                var issueView = new StockOut.StockOutIssueView(viewModel);
                MainFrame.Navigate(issueView);
            };
            
            viewModel.OnNavigateBackToList = () =>
            {
                var listView = new StockOut.StockOutListView(viewModel);
                MainFrame.Navigate(listView);
            };
            
            var listView = new StockOut.StockOutListView(viewModel);
            MainFrame.Navigate(listView);
        }

        private void BtnInventory_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = App.ServiceProvider.GetRequiredService<InventoryViewModel>();
            var inventoryView = new Inventory.InventoryView(viewModel);
            MainFrame.Navigate(inventoryView);
        }

    }
}
