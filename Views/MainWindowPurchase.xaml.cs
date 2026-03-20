using System.Windows;
using EWMS_WPF.BLL;
using Microsoft.Extensions.DependencyInjection;

namespace EWMS_WPF.Views
{
    public partial class MainWindowPurchase : Window
    {
        private readonly SessionService _sessionService;

        public MainWindowPurchase(SessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService;
            LoadUserInfo();
            
            // Auto load Purchase Orders on startup
            Loaded += (s, e) => BtnPurchaseOrders_Click(s, e);
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

        private void BtnPurchaseOrders_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = App.ServiceProvider.GetRequiredService<PurchaseOrderViewModel>();
            var listView = new PurchaseOrder.PurchaseOrderListView(viewModel);
            
            viewModel.OnNavigateToDetails = (id) =>
            {
                var detailsView = App.ServiceProvider.GetRequiredService<PurchaseOrder.PurchaseOrderDetailsView>();
                detailsView.LoadPurchaseOrder(id);
                detailsView.OnBackToList = () => BtnPurchaseOrders_Click(sender, e);
                MainFrame.Navigate(detailsView);
            };

            viewModel.OnNavigateToCreate = () =>
            {
                var createViewModel = App.ServiceProvider.GetRequiredService<PurchaseOrderCreateViewModel>();
                var createView = new PurchaseOrder.PurchaseOrderCreateView(createViewModel);
                
                createViewModel.OnSaveSuccess = () => BtnPurchaseOrders_Click(sender, e);
                createViewModel.OnCancel = () => BtnPurchaseOrders_Click(sender, e);
                
                MainFrame.Navigate(createView);
            };
            
            MainFrame.Navigate(listView);
        }

    }
}
