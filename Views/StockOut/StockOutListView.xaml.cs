using System.Windows;
using System.Windows.Controls;
using EWMS_WPF.BLL;

namespace EWMS_WPF.Views.StockOut
{
    public partial class StockOutListView : UserControl
    {
        private readonly StockOutViewModel _viewModel;

        public StockOutListView(StockOutViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.LoadPendingOrdersAsync();
        }

        private void BtnIssue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int salesOrderId)
            {
                _viewModel.OnNavigateToIssue?.Invoke(salesOrderId);
            }
        }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OnNavigateToHistory?.Invoke();
        }
    }
}
