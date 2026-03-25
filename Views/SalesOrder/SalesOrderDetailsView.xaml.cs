using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EWMS_WPF.DAL;
using Microsoft.EntityFrameworkCore;

namespace EWMS_WPF.Views.SalesOrder
{
    public partial class SalesOrderDetailsView : UserControl
    {
        private readonly IUnitOfWork _unitOfWork;

        public SalesOrderDetailsView(IUnitOfWork unitOfWork)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
        }

        public async void LoadSalesOrder(int salesOrderId)
        {
            var so = await _unitOfWork.SalesOrders.GetQueryable()
                .Include(s => s.SalesOrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(s => s.SalesOrderId == salesOrderId);

            if (so != null)
            {
                txtTitle.Text = $"Sales Order SO-{so.SalesOrderId:D4}";
                txtStatus.Text = $"Status: {so.Status}";
                txtStatus.Foreground = so.Status switch
                {
                    "Pending" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                    "Completed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    "Cancelled" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                    _ => Brushes.Gray
                };

                txtCustomer.Text = so.CustomerName;
                txtPhone.Text = so.CustomerPhone ?? "N/A";
                txtTotalAmount.Text = $"{so.TotalAmount:N0} VND";

                dgProducts.ItemsSource = so.SalesOrderDetails.ToList();
            }
        }

        public System.Action? OnBackToList { get; set; }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            OnBackToList?.Invoke();
        }
    }
}
