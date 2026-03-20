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
    public partial class PurchaseOrderCreateViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<Supplier> _suppliers = new();

        [ObservableProperty]
        private Supplier? _selectedSupplier;

        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [ObservableProperty]
        private Product? _selectedProduct;

        [ObservableProperty]
        private int _quantity = 1;

        [ObservableProperty]
        private decimal _unitPrice = 0;

        [ObservableProperty]
        private DateTime _expectedDate = DateTime.Now.AddDays(7);

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ProductLineItem> _productItems = new();

        [ObservableProperty]
        private bool _isLoading;

        public PurchaseOrderCreateViewModel(IUnitOfWork unitOfWork, SessionService sessionService)
        {
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
        }

        public async Task InitializeAsync()
        {
            await LoadSuppliersAsync();
            await LoadProductsAsync();
        }

        private async Task LoadSuppliersAsync()
        {
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            Suppliers = new ObservableCollection<Supplier>(suppliers.ToList());
        }

        private async Task LoadProductsAsync()
        {
            var products = await _unitOfWork.Products.GetQueryable()
                .Include(p => p.Category)
                .ToListAsync();
            Products = new ObservableCollection<Product>(products);
        }

        [RelayCommand]
        private void AddProduct()
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

            // Auto get unit price from product's cost price
            var unitPrice = SelectedProduct.CostPrice ?? 0;
            if (unitPrice <= 0)
            {
                MessageBox.Show("Product cost price is not set. Please update product information first.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = ProductItems.FirstOrDefault(p => p.ProductId == SelectedProduct.ProductId);
            if (existing != null)
            {
                existing.Quantity = Quantity;
                existing.UnitPrice = unitPrice;
                existing.TotalPrice = Quantity * unitPrice;
            }
            else
            {
                ProductItems.Add(new ProductLineItem
                {
                    ProductId = SelectedProduct.ProductId,
                    ProductName = SelectedProduct.ProductName,
                    Quantity = Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = Quantity * unitPrice
                });
            }

            // Reset
            SelectedProduct = null;
            Quantity = 1;
        }

        [RelayCommand]
        private void RemoveProduct(int productId)
        {
            var item = ProductItems.FirstOrDefault(p => p.ProductId == productId);
            if (item != null)
            {
                ProductItems.Remove(item);
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (SelectedSupplier == null)
            {
                MessageBox.Show("Please select a supplier.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                var purchaseOrder = new PurchaseOrder
                {
                    SupplierId = SelectedSupplier.SupplierId,
                    WarehouseId = warehouseId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now,
                    ExpectedReceivingDate = ExpectedDate,
                    TotalAmount = totalAmount,
                    Notes = Notes,
                    Status = "Ordered"
                };

                await _unitOfWork.PurchaseOrders.AddAsync(purchaseOrder);
                await _unitOfWork.SaveChangesAsync();

                foreach (var item in ProductItems)
                {
                    var detail = new PurchaseOrderDetail
                    {
                        PurchaseOrderId = purchaseOrder.PurchaseOrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    };
                    await _unitOfWork.PurchaseOrderDetails.AddAsync(detail);
                }

                await _unitOfWork.SaveChangesAsync();

                MessageBox.Show($"Purchase Order PO-{purchaseOrder.PurchaseOrderId:D4} created successfully!", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                OnSaveSuccess?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating purchase order: {ex.Message}", "Error", 
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

    public partial class ProductLineItem : ObservableObject
    {
        [ObservableProperty]
        private int _productId;

        [ObservableProperty]
        private string _productName = string.Empty;

        [ObservableProperty]
        private int _quantity;

        [ObservableProperty]
        private decimal _unitPrice;

        [ObservableProperty]
        private decimal _totalPrice;
    }
}
