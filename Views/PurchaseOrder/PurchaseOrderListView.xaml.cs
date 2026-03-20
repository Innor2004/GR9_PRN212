using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EWMS_WPF.BLL;

namespace EWMS_WPF.Views.PurchaseOrder
{
    public partial class PurchaseOrderListView : UserControl
    {
        private readonly PurchaseOrderViewModel _viewModel;

        public PurchaseOrderListView(PurchaseOrderViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.LoadPurchaseOrdersCommand.ExecuteAsync(null);
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseOrderId)
            {
                _viewModel.OnNavigateToDetails?.Invoke(purchaseOrderId);
            }
        }
    }

    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status && status == "Ordered")
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
