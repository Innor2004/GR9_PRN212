using System;
using System.Collections.Generic;
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
    public partial class SalesOrderViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<SalesOrder> _salesOrders = new();

        [ObservableProperty]
        private SalesOrder? _selectedSalesOrder;

        [ObservableProperty]
        private string _statusFilter = "All";

        [ObservableProperty]
        private string _customerSearch = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public SalesOrderViewModel(IUnitOfWork unitOfWork, SessionService sessionService)
        {
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
        }

        [RelayCommand]
        private async Task LoadSalesOrdersAsync()
        {
            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                
                var query = _unitOfWork.SalesOrders.GetQueryable()
                    .Include(so => so.SalesOrderDetails)
                        .ThenInclude(sod => sod.Product)
                    .Where(so => so.WarehouseId == warehouseId);

                if (StatusFilter != "All")
                {
                    query = query.Where(so => so.Status == StatusFilter);
                }

                if (!string.IsNullOrWhiteSpace(CustomerSearch))
                {
                    query = query.Where(so => so.CustomerName.Contains(CustomerSearch) || 
                                             (so.CustomerPhone != null && so.CustomerPhone.Contains(CustomerSearch)));
                }

                var orders = await query.OrderByDescending(so => so.CreatedAt).ToListAsync();
                SalesOrders = new ObservableCollection<SalesOrder>(orders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sales orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CancelOrderAsync(int salesOrderId)
        {
            var result = MessageBox.Show("Cancel this order?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var so = await _unitOfWork.SalesOrders.GetByIdAsync(salesOrderId);
                    if (so != null && so.Status == "Pending")
                    {
                        so.Status = "Cancelled";
                        _unitOfWork.SalesOrders.Update(so);
                        await _unitOfWork.SaveChangesAsync();

                        MessageBox.Show("Order cancelled!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadSalesOrdersAsync();
                    }
                    else
                    {
                        MessageBox.Show("Cannot cancel this order.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public async Task<Dictionary<int, int>> CheckInventoryAsync(Dictionary<int, int> productQuantities)
        {
            var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
            var result = new Dictionary<int, int>();

            foreach (var item in productQuantities)
            {
                var availableQty = await _unitOfWork.Inventories.GetQueryable()
                    .Where(inv => inv.ProductId == item.Key && inv.Location.WarehouseId == warehouseId)
                    .SumAsync(inv => inv.Quantity ?? 0);

                result[item.Key] = availableQty;
            }

            return result;
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
