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
    public partial class SalesOrderCreateViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;
        private readonly SalesOrderViewModel _salesOrderViewModel;

        [ObservableProperty]
        private string _customerName = string.Empty;

        [ObservableProperty]
        private string _customerPhone = string.Empty;

        [ObservableProperty]
        private string _customerAddress = string.Empty;

        [ObservableProperty]
        private DateTime _expectedDeliveryDate = DateTime.Now.AddDays(3);

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private Product? _selectedProduct;

        [ObservableProperty]
        private int _quantity = 1;

        [ObservableProperty]
        private ObservableCollection<SalesProductLineItem> _productItems = new();

        [ObservableProperty]
        private string _inventoryWarning = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public SalesOrderCreateViewModel(IUnitOfWork unitOfWork, SessionService sessionService, SalesOrderViewModel salesOrderViewModel)
        {
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
            _salesOrderViewModel = salesOrderViewModel;
        }

        public async Task InitializeAsync()
        {
            var products = await _unitOfWork.Products.GetQueryable()
                .Include(p => p.Category)
                .ToListAsync();
            Products = new ObservableCollection<Product>(products);
        }

        [RelayCommand]
        private async Task AddProductAsync()
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show("Please select a product.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Quantity <= 0)
            {
                MessageBox.Show("Quantity must be greater than 0.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var inventoryCheck = await _salesOrderViewModel.CheckInventoryAsync(
                    new System.Collections.Generic.Dictionary<int, int> { { SelectedProduct.ProductId, Quantity } });

                var availableQty = inventoryCheck.ContainsKey(SelectedProduct.ProductId) 
                    ? inventoryCheck[SelectedProduct.ProductId] 
                    : 0;

                if (availableQty < Quantity)
                {
                    var result = MessageBox.Show(
                        $"Warning: Only {availableQty} units available in stock.\n" +
                        $"You are requesting {Quantity} units.\n\n" +
                        "Continue anyway?", 
                        "Insufficient Stock", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return;
                }

                var existing = ProductItems.FirstOrDefault(p => p.ProductId == SelectedProduct.ProductId);
                if (existing != null)
                {
                    existing.Quantity = Quantity;
                    existing.AvailableQty = availableQty;
                    existing.TotalPrice = Quantity * existing.UnitPrice;
                }
                else
                {
                    var unitPrice = SelectedProduct.SellingPrice ?? 0;
                    ProductItems.Add(new SalesProductLineItem
                    {
                        ProductId = SelectedProduct.ProductId,
                        ProductName = SelectedProduct.ProductName,
                        AvailableQty = availableQty,
                        Quantity = Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = Quantity * unitPrice
                    });
                }

                CheckOverallInventory();
                SelectedProduct = null;
                Quantity = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void RemoveProduct(int productId)
        {
            var item = ProductItems.FirstOrDefault(p => p.ProductId == productId);
            if (item != null)
            {
                ProductItems.Remove(item);
                CheckOverallInventory();
            }
        }

        private void CheckOverallInventory()
        {
            var hasInsufficientStock = ProductItems.Any(p => p.Quantity > p.AvailableQty);
            InventoryWarning = hasInsufficientStock 
                ? "⚠️ Warning: Some products have insufficient stock!" 
                : string.Empty;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                MessageBox.Show("Please enter customer name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CustomerPhone))
            {
                MessageBox.Show("Please enter customer phone.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProductItems.Count == 0)
            {
                MessageBox.Show("Please add at least one product.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                var userId = _sessionService.CurrentSession?.UserId ?? 0;
                var totalAmount = ProductItems.Sum(p => p.TotalPrice);

                var salesOrder = new SalesOrder
                {
                    WarehouseId = warehouseId,
                    CustomerName = CustomerName.Trim(),
                    CustomerPhone = CustomerPhone.Trim(),
                    CustomerAddress = CustomerAddress.Trim(),
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now,
                    ExpectedDeliveryDate = ExpectedDeliveryDate,
                    TotalAmount = totalAmount,
                    Notes = Notes.Trim(),
                    Status = "Pending"
                };

                await _unitOfWork.SalesOrders.AddAsync(salesOrder);
                await _unitOfWork.SaveChangesAsync();

                foreach (var item in ProductItems)
                {
                    var detail = new SalesOrderDetail
                    {
                        SalesOrderId = salesOrder.SalesOrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    };
                    await _unitOfWork.SalesOrderDetails.AddAsync(detail);
                }

                await _unitOfWork.SaveChangesAsync();

                MessageBox.Show($"Sales Order SO-{salesOrder.SalesOrderId:D4} created successfully!", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                OnSaveSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating sales order: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public Action? OnSaveSuccess { get; set; }
        public Action? OnCancel { get; set; }
    }

    public partial class SalesProductLineItem : ObservableObject
    {
        [ObservableProperty]
        private int _productId;

        [ObservableProperty]
        private string _productName = string.Empty;

        [ObservableProperty]
        private int _availableQty;

        [ObservableProperty]
        private int _quantity;

        [ObservableProperty]
        private decimal _unitPrice;

        [ObservableProperty]
        private decimal _totalPrice;
    }
}
