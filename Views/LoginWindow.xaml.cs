using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using EWMS_WPF.BLL;
using Microsoft.Extensions.DependencyInjection;

namespace EWMS_WPF.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.OnLoginSuccess = () =>
            {
                Dispatcher.Invoke(() =>
                {
                    var session = App.ServiceProvider.GetRequiredService<SessionService>().CurrentSession;
                    Window? mainWindow = null;

                    if (session != null)
                    {
                        var role = session.RoleName.ToLower();
                        
                        if (role.Contains("purchase"))
                        {
                            mainWindow = App.ServiceProvider.GetRequiredService<MainWindowPurchase>();
                        }
                        else if (role.Contains("sales"))
                        {
                            mainWindow = App.ServiceProvider.GetRequiredService<MainWindowSales>();
                        }
                        else if (role.Contains("inventory"))
                        {
                            mainWindow = App.ServiceProvider.GetRequiredService<MainWindowInventory>();
                        }
                        else if(role.Contains("admin"))
                        {
                            mainWindow = App.ServiceProvider.GetRequiredService<MainWindowAdmin>();
                        }
                        else
                        {
                            MessageBox.Show("Unknown role. Please contact administrator.", "Error");
                            return;
                        }

                        mainWindow?.Show();
                        this.Close();
                    }
                });
            };

            // Important: Handle password from PasswordBox
            txtPassword.PasswordChanged += (s, e) =>
            {
                _viewModel.Password = txtPassword.Password;
            };
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.Password = txtPassword.Password;
                _viewModel.LoginCommand.Execute(null);
            }
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LoadingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Logging in..." : "LOGIN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
