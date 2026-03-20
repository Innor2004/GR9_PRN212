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
    public partial class InventoryViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private ObservableCollection<string> _racks = new();

        [ObservableProperty]
        private string? _selectedRack;

        [ObservableProperty]
        private ObservableCollection<InventoryDisplayItem> _inventoryItems = new();

        [ObservableProperty]
        private int _totalProducts;

        [ObservableProperty]
        private int _totalQuantity;

        [ObservableProperty]
        private int _totalLocations;

        [ObservableProperty]
        private bool _isLoading;

        public InventoryViewModel(IUnitOfWork unitOfWork, SessionService sessionService)
        {
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
        }

        public async Task InitializeAsync()
        {
            // Run sequentially to avoid DbContext threading issues
            await LoadSummaryAsync();
            await LoadRacksAsync();
            
            // Load inventory for the first rack if available
            if (!string.IsNullOrEmpty(SelectedRack))
            {
                await LoadInventoryAsync();
            }
        }

        private async Task LoadSummaryAsync()
        {
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;

                TotalProducts = await _unitOfWork.Inventories.GetQueryable()
                    .Include(inv => inv.Location)
                    .Where(inv => inv.Location.WarehouseId == warehouseId)
                    .Select(inv => inv.ProductId)
                    .Distinct()
                    .CountAsync();

                TotalQuantity = await _unitOfWork.Inventories.GetQueryable()
                    .Include(inv => inv.Location)
                    .Where(inv => inv.Location.WarehouseId == warehouseId)
                    .SumAsync(inv => inv.Quantity ?? 0);

                TotalLocations = await _unitOfWork.Locations.GetQueryable()
                    .Where(l => l.WarehouseId == warehouseId)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading summary: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadRacksAsync()
        {
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;

                var racks = await _unitOfWork.Locations.GetQueryable()
                    .Where(l => l.WarehouseId == warehouseId && l.Rack != null)
                    .Select(l => l.Rack!)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToListAsync();

                Racks = new ObservableCollection<string>(racks);
                if (Racks.Any())
                {
                    SelectedRack = Racks.First();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading racks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LoadInventoryAsync()
        {
            if (string.IsNullOrEmpty(SelectedRack)) return;

            IsLoading = true;
            try
            {
                var warehouseId = _sessionService.CurrentSession?.WarehouseId ?? 0;

                var locations = await _unitOfWork.Locations.GetQueryable()
                    .Include(l => l.Inventories)
                        .ThenInclude(inv => inv.Product)
                            .ThenInclude(p => p.Category)
                    .Where(l => l.WarehouseId == warehouseId && l.Rack == SelectedRack)
                    .OrderBy(l => l.LocationCode)
                    .ToListAsync();

                var items = locations.SelectMany(loc =>
                    loc.Inventories.Where(inv => inv.Quantity > 0).Select(inv => new InventoryDisplayItem
                    {
                        LocationCode = loc.LocationCode,
                        ProductName = inv.Product.ProductName,
                        CategoryName = inv.Product.Category?.CategoryName ?? "N/A",
                        Quantity = inv.Quantity ?? 0,
                        LastUpdated = inv.LastUpdated ?? DateTime.Now
                    })
                ).ToList();

                InventoryItems = new ObservableCollection<InventoryDisplayItem>(items);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading inventory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public partial class InventoryDisplayItem : ObservableObject
    {
        [ObservableProperty]
        private string _locationCode = string.Empty;

        [ObservableProperty]
        private string _productName = string.Empty;

        [ObservableProperty]
        private string _categoryName = string.Empty;

        [ObservableProperty]
        private int _quantity;

        [ObservableProperty]
        private DateTime _lastUpdated;
    }
}
