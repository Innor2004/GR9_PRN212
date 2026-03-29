using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EWMS_WPF.BLL;
using EWMS_WPF.Views.Admin;
using Microsoft.Extensions.DependencyInjection;
namespace EWMS_WPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindowAdmin.xaml
    /// </summary>
    public partial class MainWindowAdmin : Window
    {
        private readonly SessionService _sessionService;
        public MainWindowAdmin(SessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService;
            LoadUserInfo();
            Loaded += (s,e) => UserButton_Click(s,e);
        }

        private void LoadUserInfo()
        {
            var session = _sessionService.CurrentSession;
            if (session != null)
            {
                txtUserInfo.Text = $"{session.FullName} - {session.WarehouseName}";
            }
        }
        private void UserButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = App.ServiceProvider.GetRequiredService<UserCRUDViewModel>();
            var listView = new UserCreate(viewModel);
            MainFrame.Navigate(listView);
        }

        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = App.ServiceProvider.GetRequiredService<ProductCRUDViewModel>();
            var listView = new ProductCRUD(viewModel);
            MainFrame.Navigate(listView);
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn đăng xuất?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _sessionService.ClearSession();
            var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            Close();
        }
    }
}
