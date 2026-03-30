using System.Windows;
using System.Windows.Controls;
using EWMS_WPF.BLL;

namespace EWMS_WPF.Views.StockIn
{
    public partial class StockInListView : UserControl
    {
        private readonly StockInViewModel _viewModel;

        public StockInListView(StockInViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.LoadPendingOrdersAsync();
        }

        private void BtnReceive_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int purchaseOrderId)
            {
                _viewModel.OnNavigateToReceive?.Invoke(purchaseOrderId);
            }
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OnNavigateToHistory?.Invoke();
        }
    }
}
