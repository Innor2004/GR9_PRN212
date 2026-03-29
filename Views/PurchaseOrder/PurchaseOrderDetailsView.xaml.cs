using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EWMS_WPF.BLL;
using EWMS_WPF.DAL;
using Microsoft.EntityFrameworkCore;

namespace EWMS_WPF.Views.PurchaseOrder
{
    public partial class PurchaseOrderDetailsView : UserControl
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;

        public PurchaseOrderDetailsView(IUnitOfWork unitOfWork, SessionService sessionService)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
        }

        public async void LoadPurchaseOrder(int purchaseOrderId)
        {
            var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
            var po = await _unitOfWork.PurchaseOrders.GetQueryable()
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId && p.WarehouseId == warehouseId);

            if (po != null)
            {
                txtTitle.Text = $"Purchase Order PO-{po.PurchaseOrderId:D4}";
                txtStatus.Text = $"Status: {po.Status}";
                txtStatus.Foreground = po.Status switch
                {
                    "Ordered" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                    "ReadyToReceive" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                    "Received" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    "Cancelled" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                    _ => Brushes.Gray
                };

                txtSupplier.Text = po.Supplier?.SupplierName ?? "N/A";
                txtCreatedDate.Text = po.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                txtTotalAmount.Text = $"{po.TotalAmount:N0} VND";
                txtNotes.Text = string.IsNullOrWhiteSpace(po.Notes) ? "No notes" : po.Notes;

                dgProducts.ItemsSource = po.PurchaseOrderDetails.ToList();
            }
        }

        public System.Action? OnBackToList { get; set; }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            OnBackToList?.Invoke();
        }
    }
}
