using System.Windows.Controls;
using EWMS_WPF.BLL;

namespace EWMS_WPF.Views.Inventory
{
    public partial class InventoryView : UserControl
    {
        private readonly InventoryViewModel _viewModel;
        private bool _isInitializing = false;

        public InventoryView(InventoryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) =>
            {
                _isInitializing = true;
                await _viewModel.InitializeAsync();
                _isInitializing = false;
            };

            _viewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.SelectedRack) && !_isInitializing)
                {
                    await _viewModel.LoadInventoryAsync();
                }
            };
        }
    }
}
