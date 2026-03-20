using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EWMS_WPF.DAL;
using EWMS_WPF.Models;
using Microsoft.EntityFrameworkCore;

namespace EWMS_WPF.BLL
{
    public partial class PurchaseOrderViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<PurchaseOrder> _purchaseOrders = new();

        [ObservableProperty]
        private PurchaseOrder? _selectedPurchaseOrder;

        [ObservableProperty]
        private string _statusFilter = "All";

        [ObservableProperty]
        private bool _isLoading;

        public PurchaseOrderViewModel(IUnitOfWork unitOfWork, SessionService sessionService)
        {
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
        }

        [RelayCommand]
        private async Task LoadPurchaseOrdersAsync()
        {
            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                
                var query = _unitOfWork.PurchaseOrders.GetQueryable()
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Product)
                    .Where(po => po.WarehouseId == warehouseId);

                if (StatusFilter != "All")
                {
                    query = query.Where(po => po.Status == StatusFilter);
                }

                var orders = await query.OrderByDescending(po => po.CreatedAt).ToListAsync();
                PurchaseOrders = new ObservableCollection<PurchaseOrder>(orders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading purchase orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task MarkAsDeliveredAsync(int purchaseOrderId)
        {
            var result = MessageBox.Show("Mark this order as delivered?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                    var po = await _unitOfWork.PurchaseOrders.FirstOrDefaultAsync(
                        p => p.PurchaseOrderId == purchaseOrderId && p.WarehouseId == warehouseId);

                    if (po != null && po.Status == "Ordered")
                    {
                        po.Status = "ReadyToReceive";
                        _unitOfWork.PurchaseOrders.Update(po);
                        await _unitOfWork.SaveChangesAsync();

                        MessageBox.Show("Order marked as delivered!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadPurchaseOrdersAsync();
                    }
                    else
                    {
                        MessageBox.Show("Cannot update order status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task CancelOrderAsync(int purchaseOrderId)
        {
            var result = MessageBox.Show("Cancel this order?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                    var po = await _unitOfWork.PurchaseOrders.FirstOrDefaultAsync(
                        p => p.PurchaseOrderId == purchaseOrderId && p.WarehouseId == warehouseId);

                    if (po != null && po.Status == "Ordered")
                    {
                        po.Status = "Cancelled";
                        _unitOfWork.PurchaseOrders.Update(po);
                        await _unitOfWork.SaveChangesAsync();

                        MessageBox.Show("Order cancelled!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadPurchaseOrdersAsync();
                    }
                    else
                    {
                        MessageBox.Show("Cannot cancel this order. Only orders with 'Ordered' status can be cancelled.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void NavigateToCreate()
        {
            OnNavigateToCreate?.Invoke();
        }

        public Action<int>? OnNavigateToDetails { get; set; }
        public Action? OnNavigateToCreate { get; set; }
    }
}
