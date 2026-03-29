using System.Windows;
using EWMS_WPF.BLL;
using Microsoft.Extensions.DependencyInjection;

namespace EWMS_WPF.Views
{
    public partial class MainWindowSales : Window
    {
        private readonly SessionService _sessionService;

        public MainWindowSales(SessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService;
            LoadUserInfo();
            
            // Auto load Sales Orders on startup
            Loaded += (s, e) => BtnSalesOrders_Click(s, e);
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

        private void BtnSalesOrders_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = App.ServiceProvider.GetRequiredService<SalesOrderViewModel>();
            var listView = new SalesOrder.SalesOrderListView(viewModel);
            
            viewModel.OnNavigateToDetails = (id) =>
            {
                var detailsView = App.ServiceProvider.GetRequiredService<SalesOrder.SalesOrderDetailsView>();
                detailsView.LoadSalesOrder(id);
                detailsView.OnBackToList = () => BtnSalesOrders_Click(sender, e);
                MainFrame.Navigate(detailsView);
            };

            viewModel.OnNavigateToCreate = () =>
            {
                var createViewModel = App.ServiceProvider.GetRequiredService<SalesOrderCreateViewModel>();
                var createView = new SalesOrder.SalesOrderCreateView(createViewModel);
                
                createViewModel.OnSaveSuccess = () => BtnSalesOrders_Click(sender, e);
                createViewModel.OnCancel = () => BtnSalesOrders_Click(sender, e);
                
                MainFrame.Navigate(createView);
            };

            viewModel.OnNavigateToEdit = (id) =>
            {
                var editViewModel = App.ServiceProvider.GetRequiredService<SalesOrderEditViewModel>();
                var editView = new SalesOrder.SalesOrderEditView(editViewModel);

                editViewModel.OnSaveSuccess = () => BtnSalesOrders_Click(sender, e);
                editViewModel.OnCancel = () => BtnSalesOrders_Click(sender, e);

                MainFrame.Navigate(editView);
                editView.LoadSalesOrder(id);
            };
            
            MainFrame.Navigate(listView);
        }

    }
}
