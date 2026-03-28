using System.Windows;
using System.Windows.Controls;
using EWMS_WPF.BLL;

namespace EWMS_WPF.Views.SalesOrder
{
    public partial class SalesOrderEditView : UserControl
    {
        private readonly SalesOrderEditViewModel _viewModel;

        public SalesOrderEditView(SalesOrderEditViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        public async void LoadSalesOrder(int salesOrderId)
        {
            await _viewModel.LoadAsync(salesOrderId);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Discard changes and go back?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.OnCancel?.Invoke();
            }
        }
    }
}
