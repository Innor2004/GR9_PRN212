using System.Windows.Controls;
using EWMS_WPF.BLL;

namespace EWMS_WPF.Views.StockIn
{
    public partial class StockInReceiveView : UserControl
    {
        public StockInReceiveView(StockInViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
