using System.Windows.Controls;
using EWMS_WPF.BLL;

namespace EWMS_WPF.Views.StockOut
{
    public partial class StockOutIssueView : UserControl
    {
        public StockOutIssueView(StockOutViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
