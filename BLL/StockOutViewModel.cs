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
    public partial class StockOutViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<SalesOrder> _pendingSalesOrders = new();

        [ObservableProperty]
        private ObservableCollection<SalesOrder> _historyOrders = new();

        [ObservableProperty]
        private SalesOrder? _salesOrder;

        [ObservableProperty]
        private string _salesOrderInfo = string.Empty;

        [ObservableProperty]
        private ObservableCollection<IssueLineItem> _issueItems = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        private ObservableCollection<SalesOrder> _allSalesOrders = new();

        public StockOutViewModel(IUnitOfWork unitOfWork, SessionService sessionService)
        {
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
        }

        public async Task LoadPendingOrdersAsync()
        {
            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                
                var orders = await _unitOfWork.SalesOrders.GetQueryable()
                    .Include(so => so.SalesOrderDetails)
                        .ThenInclude(sod => sod.Product)
                    .Where(so => so.WarehouseId == warehouseId && 
                                (so.Status == "Pending" || so.Status == "PartiallyIssued"))
                    .OrderByDescending(so => so.CreatedAt)
                    .ToListAsync();

                _allSalesOrders = new ObservableCollection<SalesOrder>(orders);
                PendingSalesOrders = _allSalesOrders;
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
                PendingSalesOrders = _allSalesOrders;
                HistoryOrders = _allSalesOrders;
                return;
            }

            if (int.TryParse(SearchText.Trim(), out int soId))
            {
                var filtered = _allSalesOrders.Where(so => so.SalesOrderId == soId).ToList();
                PendingSalesOrders = new ObservableCollection<SalesOrder>(filtered);
                HistoryOrders = new ObservableCollection<SalesOrder>(filtered);
            }
            else
            {
                MessageBox.Show("Please enter a valid Sales Order ID (number)", "Invalid Search", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public async Task LoadSalesOrderForIssueAsync(int salesOrderId)
        {
            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;

                // Load SO with all details
                SalesOrder = await _unitOfWork.SalesOrders.GetQueryable()
                    .Include(so => so.SalesOrderDetails)
                        .ThenInclude(sod => sod.Product)
                    .FirstOrDefaultAsync(so => so.SalesOrderId == salesOrderId && so.WarehouseId == warehouseId);

                if (SalesOrder == null)
                {
                    MessageBox.Show("Sales Order not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    OnNavigateBackToList?.Invoke();
                    return;
                }

                SalesOrderInfo = $"SO #{SalesOrder.SalesOrderId} - {SalesOrder.CustomerName}";

                // Load all inventory for this warehouse
                var allInventory = await _unitOfWork.Inventories.GetQueryable()
                    .Include(inv => inv.Location)
                    .Where(inv => inv.Location.WarehouseId == warehouseId)
                    .ToListAsync();

                // Get already issued quantities from StockOutDetails
                var issuedQuantities = await _unitOfWork.StockOutDetails.GetQueryable()
                    .Where(sod => sod.StockOut.SalesOrderId == salesOrderId)
                    .GroupBy(sod => sod.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalIssued = g.Sum(x => x.Quantity) })
                    .ToListAsync();

                // Create issue items
                var items = new ObservableCollection<IssueLineItem>();
                foreach (var detail in SalesOrder.SalesOrderDetails)
                {
                    var alreadyIssued = issuedQuantities.FirstOrDefault(r => r.ProductId == detail.ProductId)?.TotalIssued ?? 0;
                    var remaining = detail.Quantity - alreadyIssued;

                    if (remaining <= 0) // Skip items that are fully issued
                        continue;

                    // Get inventory for this product
                    var productInventory = allInventory.Where(inv => inv.ProductId == detail.ProductId).ToList();
                    var totalAvailable = productInventory.Sum(inv => inv.Quantity ?? 0);

                    var item = new IssueLineItem
                    {
                        ProductId = detail.ProductId,
                        Product = detail.Product,
                        OrderedQuantity = detail.Quantity,
                        AlreadyIssuedQuantity = alreadyIssued,
                        RemainingQuantity = remaining,
                        AvailableStock = totalAvailable,
                        IssueQuantity = Math.Min(remaining, totalAvailable), // Default to min of remaining and available
                        AllInventories = productInventory
                    };

                    // Setup location tracking
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(IssueLineItem.SelectedLocation))
                        {
                            var issueItem = s as IssueLineItem;
                            if (issueItem?.SelectedLocation != null)
                            {
                                var inventory = issueItem.AllInventories.FirstOrDefault(inv => inv.LocationId == issueItem.SelectedLocation.LocationId);
                                issueItem.StockAtLocation = inventory?.Quantity ?? 0;
                            }
                        }
                    };

                    // Get locations that have stock for this product
                    var locationsWithStock = productInventory
                        .Where(inv => (inv.Quantity ?? 0) > 0)
                        .Select(inv => inv.Location)
                        .ToList();

                    item.AvailableLocations = new ObservableCollection<Location>(locationsWithStock);
                    item.SelectedLocation = locationsWithStock.FirstOrDefault();

                    items.Add(item);
                }

                IssueItems = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sales order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                OnNavigateBackToList?.Invoke();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ConfirmIssueAsync()
        {
            if (SalesOrder == null)
                return;

            // Validate
            if (!IssueItems.Any(item => item.IssueQuantity > 0))
            {
                MessageBox.Show("Please enter at least one item to issue!", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in IssueItems.Where(i => i.IssueQuantity > 0))
            {
                if (item.IssueQuantity > item.RemainingQuantity)
                {
                    MessageBox.Show($"Issue quantity for {item.Product.ProductName} exceeds remaining quantity!", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (item.IssueQuantity > item.AvailableStock)
                {
                    MessageBox.Show($"Issue quantity for {item.Product.ProductName} exceeds available stock!", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (item.SelectedLocation == null)
                {
                    MessageBox.Show($"Please select a location for {item.Product.ProductName}!", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (item.IssueQuantity > item.StockAtLocation)
                {
                    MessageBox.Show($"Issue quantity for {item.Product.ProductName} exceeds stock at selected location!", 
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show(
                $"Confirm issuing goods for SO #{SalesOrder.SalesOrderId}?",
                "Confirm Issue",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                var userId = _sessionService.CurrentSession?.UserId ?? 0;

                // Create StockOutReceipt
                var stockOutReceipt = new StockOutReceipt
                {
                    SalesOrderId = SalesOrder.SalesOrderId,
                    WarehouseId = warehouseId,
                    IssuedBy = userId,
                    IssuedDate = DateTime.Now,
                    Reason = $"Issued for SO #{SalesOrder.SalesOrderId}",
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.StockOutReceipts.AddAsync(stockOutReceipt);
                await _unitOfWork.SaveChangesAsync();

                // Create StockOutDetails and update Inventory
                foreach (var item in IssueItems.Where(i => i.IssueQuantity > 0))
                {
                    // Add StockOutDetail
                    var stockOutDetail = new StockOutDetail
                    {
                        StockOutId = stockOutReceipt.StockOutId,
                        ProductId = item.ProductId,
                        LocationId = item.SelectedLocation!.LocationId,
                        Quantity = item.IssueQuantity,
                        UnitPrice = 0, // You may want to get this from SalesOrderDetail
                        TotalPrice = 0
                    };
                    await _unitOfWork.StockOutDetails.AddAsync(stockOutDetail);

                    // Update Inventory - Decrease quantity
                    var inventory = await _unitOfWork.Inventories.GetQueryable()
                        .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId && 
                                                   inv.LocationId == item.SelectedLocation.LocationId);

                    if (inventory != null)
                    {
                        inventory.Quantity -= item.IssueQuantity;
                        inventory.LastUpdated = DateTime.Now;
                        _unitOfWork.Inventories.Update(inventory);
                    }
                }

                // Update SalesOrder status - Check if all items are fully issued
                var totalOrdered = SalesOrder.SalesOrderDetails.Sum(d => d.Quantity);
                var totalIssued = await _unitOfWork.StockOutDetails.GetQueryable()
                    .Where(sod => sod.StockOut.SalesOrderId == SalesOrder.SalesOrderId)
                    .SumAsync(sod => sod.Quantity);

                totalIssued += IssueItems.Where(i => i.IssueQuantity > 0).Sum(i => i.IssueQuantity);

                if (totalIssued >= totalOrdered)
                {
                    SalesOrder.Status = "Completed";
                }
                else
                {
                    SalesOrder.Status = "PartiallyIssued";
                }

                _unitOfWork.SalesOrders.Update(SalesOrder);

                await _unitOfWork.SaveChangesAsync();

                MessageBox.Show("Goods issued successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                OnNavigateBackToList?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error issuing goods: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task LoadHistoryOrdersAsync()
        {
            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                
                var orders = await _unitOfWork.SalesOrders.GetQueryable()
                    .Include(so => so.SalesOrderDetails)
                        .ThenInclude(sod => sod.Product)
                    .Where(so => so.WarehouseId == warehouseId && 
                                (so.Status == "Completed" || so.Status == "Cancelled"))
                    .OrderByDescending(so => so.CreatedAt)
                    .ToListAsync();

                _allSalesOrders = new ObservableCollection<SalesOrder>(orders);
                HistoryOrders = _allSalesOrders;
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
        private void BackToList()
        {
            OnNavigateBackToList?.Invoke();
        }

        public Action<int>? OnNavigateToIssue { get; set; }
        public Action? OnNavigateBackToList { get; set; }
        public Action? OnNavigateToHistory { get; set; }
    }

    public partial class IssueLineItem : ObservableObject
    {
        [ObservableProperty]
        private int _productId;

        [ObservableProperty]
        private Product _product = null!;

        [ObservableProperty]
        private int _orderedQuantity;

        [ObservableProperty]
        private int _alreadyIssuedQuantity;

        [ObservableProperty]
        private int _remainingQuantity;

        [ObservableProperty]
        private int _availableStock;

        [ObservableProperty]
        private int _issueQuantity;

        [ObservableProperty]
        private Location? _selectedLocation;

        [ObservableProperty]
        private ObservableCollection<Location> _availableLocations = new();

        [ObservableProperty]
        private int _stockAtLocation;

        public List<Inventory> AllInventories { get; set; } = new();
    }
}
