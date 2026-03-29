using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public partial class StockInViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<PurchaseOrder> _pendingPurchaseOrders = new();

        [ObservableProperty]
        private PurchaseOrder? _purchaseOrder;

        [ObservableProperty]
        private string _purchaseOrderInfo = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ReceiveLineItem> _receiveItems = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        private ObservableCollection<PurchaseOrder> _allPurchaseOrders = new();

        public StockInViewModel(IUnitOfWork unitOfWork, SessionService sessionService)
        {
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
        }

        [RelayCommand]
        private async Task LoadPendingOrdersAsync()
        {
            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                
                var orders = await _unitOfWork.PurchaseOrders.GetQueryable()
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Product)
                    .Where(po => po.WarehouseId == warehouseId && 
                                (po.Status == "ReadyToReceive" || po.Status == "PartiallyReceived"))
                    .OrderByDescending(po => po.CreatedAt)
                    .ToListAsync();

                _allPurchaseOrders = new ObservableCollection<PurchaseOrder>(orders);
                PendingPurchaseOrders = _allPurchaseOrders;
                SearchText = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Search()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                PendingPurchaseOrders = _allPurchaseOrders;
                return;
            }

            if (int.TryParse(SearchText.Trim(), out int poId))
            {
                var filtered = _allPurchaseOrders.Where(po => po.PurchaseOrderId == poId).ToList();
                PendingPurchaseOrders = new ObservableCollection<PurchaseOrder>(filtered);
            }
            else
            {
                MessageBox.Show("Please enter a valid Purchase Order ID (number)", "Invalid Search", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public async Task LoadPurchaseOrderForReceiveAsync(int purchaseOrderId)
        {
            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;

                // Load PO with all details
                PurchaseOrder = await _unitOfWork.PurchaseOrders.GetQueryable()
                    .Include(po => po.Supplier)
                    .Include(po => po.PurchaseOrderDetails)
                        .ThenInclude(pod => pod.Product)
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId && po.WarehouseId == warehouseId);

                if (PurchaseOrder == null)
                {
                    MessageBox.Show("Purchase Order not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    OnNavigateBackToList?.Invoke();
                    return;
                }

                PurchaseOrderInfo = $"PO #{PurchaseOrder.PurchaseOrderId} - {PurchaseOrder.Supplier?.SupplierName}";

                // Load locations for this warehouse
                var locations = await _unitOfWork.Locations.GetQueryable()
                    .Where(l => l.WarehouseId == warehouseId)
                    .ToListAsync();

                // Get already received quantities from StockInDetails
                var receivedQuantities = await _unitOfWork.StockInDetails.GetQueryable()
                    .Where(sid => sid.StockIn.PurchaseOrderId == purchaseOrderId)
                    .GroupBy(sid => sid.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalReceived = g.Sum(x => x.Quantity) })
                    .ToListAsync();

                // Create receive items
                var items = new ObservableCollection<ReceiveLineItem>();
                foreach (var detail in PurchaseOrder.PurchaseOrderDetails)
                {
                    var alreadyReceived = receivedQuantities.FirstOrDefault(r => r.ProductId == detail.ProductId)?.TotalReceived ?? 0;
                    var remaining = detail.Quantity - alreadyReceived;

                    if (remaining > 0) // Only show items that still need to be received
                    {
                        items.Add(new ReceiveLineItem
                        {
                            ProductId = detail.ProductId,
                            Product = detail.Product,
                            OrderedQuantity = detail.Quantity,
                            AlreadyReceivedQuantity = alreadyReceived,
                            RemainingQuantity = remaining,
                            ReceiveQuantity = remaining, // Default to remaining quantity
                            AvailableLocations = new ObservableCollection<Location>(locations),
                            SelectedLocation = locations.FirstOrDefault()
                        });
                    }
                }

                ReceiveItems = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading purchase order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                OnNavigateBackToList?.Invoke();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ConfirmReceiptAsync()
        {
            if (PurchaseOrder == null)
                return;

            // Validate
            if (!ReceiveItems.Any(item => item.ReceiveQuantity > 0))
            {
                MessageBox.Show("Please enter at least one item to receive!", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in ReceiveItems.Where(i => i.ReceiveQuantity > 0))
            {
                if (item.ReceiveQuantity > item.RemainingQuantity)
                {
                    MessageBox.Show($"Receive quantity for {item.Product.ProductName} exceeds remaining quantity!", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (item.SelectedLocation == null)
                {
                    MessageBox.Show($"Please select a location for {item.Product.ProductName}!", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show(
                $"Confirm receiving goods for PO #{PurchaseOrder.PurchaseOrderId}?",
                "Confirm Receipt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                var userId = _sessionService.CurrentSession?.UserId ?? 0;

                // Create StockInReceipt
                var stockInReceipt = new StockInReceipt
                {
                    PurchaseOrderId = PurchaseOrder.PurchaseOrderId,
                    WarehouseId = warehouseId,
                    ReceivedDate = DateTime.Now,
                    ReceivedBy = userId,
                    Reason = $"Received from PO #{PurchaseOrder.PurchaseOrderId}",
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.StockInReceipts.AddAsync(stockInReceipt);
                await _unitOfWork.SaveChangesAsync();

                // Create StockInDetails and update Inventory
                foreach (var item in ReceiveItems.Where(i => i.ReceiveQuantity > 0))
                {
                    // Add StockInDetail
                    var stockInDetail = new StockInDetail
                    {
                        StockInId = stockInReceipt.StockInId,
                        ProductId = item.ProductId,
                        LocationId = item.SelectedLocation!.LocationId,
                        Quantity = item.ReceiveQuantity,
                        UnitPrice = 0, // You may want to get this from PurchaseOrderDetail
                        TotalPrice = 0
                    };
                    await _unitOfWork.StockInDetails.AddAsync(stockInDetail);

                    // Update or create Inventory
                    var inventory = await _unitOfWork.Inventories.GetQueryable()
                        .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId && 
                                                   inv.LocationId == item.SelectedLocation.LocationId);

                    if (inventory != null)
                    {
                        inventory.Quantity += item.ReceiveQuantity;
                        inventory.LastUpdated = DateTime.Now;
                        _unitOfWork.Inventories.Update(inventory);
                    }
                    else
                    {
                        inventory = new Inventory
                        {
                            ProductId = item.ProductId,
                            LocationId = item.SelectedLocation.LocationId,
                            Quantity = item.ReceiveQuantity,
                            LastUpdated = DateTime.Now
                        };
                        await _unitOfWork.Inventories.AddAsync(inventory);
                    }
                }

                // Update PurchaseOrder status
                var totalOrdered = PurchaseOrder.PurchaseOrderDetails.Sum(d => d.Quantity);
                var totalReceived = await _unitOfWork.StockInDetails.GetQueryable()
                    .Where(sid => sid.StockIn.PurchaseOrderId == PurchaseOrder.PurchaseOrderId)
                    .SumAsync(sid => sid.Quantity);

                totalReceived += ReceiveItems.Where(i => i.ReceiveQuantity > 0).Sum(i => i.ReceiveQuantity);

                if (totalReceived >= totalOrdered)
                {
                    PurchaseOrder.Status = "Received";
                }
                else
                {
                    PurchaseOrder.Status = "PartiallyReceived";
                }

                _unitOfWork.PurchaseOrders.Update(PurchaseOrder);

                await _unitOfWork.SaveChangesAsync();

                MessageBox.Show("Goods received successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                OnNavigateBackToList?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving goods: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void BackToList()
        {
            OnNavigateBackToList?.Invoke();
        }

        public Action<int>? OnNavigateToReceive { get; set; }
        public Action? OnNavigateBackToList { get; set; }
    }

    public partial class ReceiveLineItem : ObservableObject
    {
        [ObservableProperty]
        private int _productId;

        [ObservableProperty]
        private Product _product = null!;

        [ObservableProperty]
        private int _orderedQuantity;

        [ObservableProperty]
        private int _alreadyReceivedQuantity;

        [ObservableProperty]
        private int _remainingQuantity;

        [ObservableProperty]
        private int _receiveQuantity;

        [ObservableProperty]
        private Location? _selectedLocation;

        [ObservableProperty]
        private ObservableCollection<Location> _availableLocations = new();
    }
}
