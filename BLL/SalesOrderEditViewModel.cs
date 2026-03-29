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
    public partial class SalesOrderEditViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;
        private readonly SalesOrderViewModel _salesOrderViewModel;

        [ObservableProperty]
        private int _salesOrderId;

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

        public SalesOrderEditViewModel(
            IUnitOfWork unitOfWork,
            SessionService sessionService,
            SalesOrderViewModel salesOrderViewModel)
        {
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
            _salesOrderViewModel = salesOrderViewModel;
        }

        public async Task LoadAsync(int salesOrderId)
        {
            IsLoading = true;
            try
            {
                var products = await _unitOfWork.Products.GetQueryable()
                    .Include(p => p.Category)
                    .ToListAsync();
                Products = new ObservableCollection<Product>(products);

                var so = await _unitOfWork.SalesOrders.GetQueryable()
                    .Include(s => s.SalesOrderDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(s => s.SalesOrderId == salesOrderId);

                if (so == null)
                {
                    MessageBox.Show("Sales order not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    OnCancel?.Invoke();
                    return;
                }

                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                if (so.WarehouseId != warehouseId)
                {
                    MessageBox.Show("You cannot edit this order (different warehouse).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    OnCancel?.Invoke();
                    return;
                }

                if (so.Status != "Pending")
                {
                    MessageBox.Show("Only pending orders can be edited.", "Not allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    OnCancel?.Invoke();
                    return;
                }

                SalesOrderId = so.SalesOrderId;
                CustomerName = so.CustomerName;
                CustomerPhone = so.CustomerPhone ?? string.Empty;
                CustomerAddress = so.CustomerAddress ?? string.Empty;
                ExpectedDeliveryDate = so.ExpectedDeliveryDate;
                Notes = so.Notes ?? string.Empty;

                ProductItems.Clear();
                foreach (var d in so.SalesOrderDetails.OrderBy(x => x.ProductId))
                {
                    var invCheck = await _salesOrderViewModel.CheckInventoryAsync(
                        new Dictionary<int, int> { { d.ProductId, d.Quantity } });
                    var availableQty = invCheck.GetValueOrDefault(d.ProductId, 0);
                    var productName = d.Product?.ProductName ?? $"Product #{d.ProductId}";
                    ProductItems.Add(new SalesProductLineItem
                    {
                        ProductId = d.ProductId,
                        ProductName = productName,
                        AvailableQty = availableQty,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        TotalPrice = d.TotalPrice ?? d.Quantity * d.UnitPrice
                    });
                }

                CheckOverallInventory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                OnCancel?.Invoke();
            }
            finally
            {
                IsLoading = false;
            }
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
                    new Dictionary<int, int> { { SelectedProduct.ProductId, Quantity } });

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

            if (!TryValidatePhone(CustomerPhone, out var phoneNorm, out var phoneErr))
            {
                MessageBox.Show(phoneErr, "Số điện thoại", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                var so = await _unitOfWork.SalesOrders.GetQueryable()
                    .Include(s => s.SalesOrderDetails)
                    .FirstOrDefaultAsync(s => s.SalesOrderId == SalesOrderId);

                if (so == null)
                {
                    MessageBox.Show("Order no longer exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (so.Status != "Pending")
                {
                    MessageBox.Show("This order can no longer be edited.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;
                if (so.WarehouseId != warehouseId)
                {
                    MessageBox.Show("Cannot save: warehouse mismatch.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var totalAmount = ProductItems.Sum(p => p.TotalPrice);

                foreach (var d in so.SalesOrderDetails.ToList())
                    _unitOfWork.SalesOrderDetails.Delete(d);

                so.CustomerName = CustomerName.Trim();
                so.CustomerPhone = phoneNorm;
                so.CustomerAddress = CustomerAddress.Trim();
                so.ExpectedDeliveryDate = ExpectedDeliveryDate;
                so.Notes = Notes.Trim();
                so.TotalAmount = totalAmount;

                _unitOfWork.SalesOrders.Update(so);
                await _unitOfWork.SaveChangesAsync();

                foreach (var item in ProductItems)
                {
                    await _unitOfWork.SalesOrderDetails.AddAsync(new SalesOrderDetail
                    {
                        SalesOrderId = SalesOrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    });
                }

                await _unitOfWork.SaveChangesAsync();

                MessageBox.Show($"Sales Order SO-{SalesOrderId:D4} updated successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                OnSaveSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving sales order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        private static bool TryValidatePhone(string? phone, out string normalized, out string error)
        {
            normalized = "";
            error = "";

            if (string.IsNullOrWhiteSpace(phone))
            {
                error = "Vui lòng nhập số điện thoại.";
                return false;
            }

            var t = phone.Trim();
            if (!t.All(char.IsDigit))
            {
                error = "Số điện thoại chỉ được nhập chữ số (0–9).";
                return false;
            }

            if (t.Length < 9 || t.Length > 10)
            {
                error = $"Số điện thoại phải có từ 9 đến 10 chữ số (hiện có {t.Length} số).";
                return false;
            }

            normalized = t;
            return true;
        }

        public Action? OnSaveSuccess { get; set; }
        public Action? OnCancel { get; set; }
    }
}
