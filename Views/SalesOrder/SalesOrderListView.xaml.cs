using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EWMS_WPF.BLL;

namespace EWMS_WPF.Views.SalesOrder
{
    public partial class SalesOrderListView : UserControl
    {
        private readonly SalesOrderViewModel _viewModel;

        public SalesOrderListView(SalesOrderViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.LoadSalesOrdersCommand.ExecuteAsync(null);
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int salesOrderId)
            {
                _viewModel.OnNavigateToDetails?.Invoke(salesOrderId);
            }
        }
    }

    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status && status == "Pending")
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
