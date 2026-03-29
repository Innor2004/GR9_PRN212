using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EWMS_WPF.DAL;
using EWMS_WPF.Models;
using EWMS_WPF.Views.Admin;
using Microsoft.EntityFrameworkCore;

namespace EWMS_WPF.BLL
{
    public class ProductRowItem
    {
        public int Index { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string Unit { get; set; } = "";
        public string CostPriceText { get; set; } = "";
        public string SellingPriceText { get; set; } = "";
        public Brush AvatarColor { get; set; } = Brushes.Gray;
        public string Initials { get; set; } = "?";
    }

    public partial class ProductCRUDViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly List<int> _categoryIdByFilterIndex = new();

        private const int PageSize = 10;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private int _categoryFilterIndex;

        [ObservableProperty]
        private ObservableCollection<string> _categoryFilterOptions = new();

        [ObservableProperty]
        private ObservableCollection<ProductRowItem> _productRows = new();

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private string _pagerInfo = "";

        [ObservableProperty]
        private bool _canPrevPage;

        [ObservableProperty]
        private bool _canNextPage;

        public ProductCRUDViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task InitializeAsync()
        {
            await LoadCategoryFilterAsync();
            await LoadProductsAsync();
        }

        partial void OnSearchTextChanged(string value) => _ = LoadProductsAsync();
        partial void OnCategoryFilterIndexChanged(int value) => _ = LoadProductsAsync();

        private async Task LoadCategoryFilterAsync()
        {
            var cats = await _unitOfWork.ProductCategories.GetQueryable()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            _categoryIdByFilterIndex.Clear();
            var opts = new List<string> { "Tất cả danh mục" };
            foreach (var c in cats)
            {
                _categoryIdByFilterIndex.Add(c.CategoryId);
                opts.Add(c.CategoryName);
            }

            CategoryFilterOptions = new ObservableCollection<string>(opts);
        }

        [RelayCommand]
        private async Task LoadProductsAsync()
        {
            var query = _unitOfWork.Products.GetQueryable()
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var t = SearchText.Trim();
                query = query.Where(p => p.ProductName.Contains(t));
            }

            if (CategoryFilterIndex > 0 && CategoryFilterIndex - 1 < _categoryIdByFilterIndex.Count)
            {
                var cid = _categoryIdByFilterIndex[CategoryFilterIndex - 1];
                query = query.Where(p => p.CategoryId == cid);
            }

            TotalCount = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;
            if (CurrentPage < 1)
                CurrentPage = 1;

            var skip = (CurrentPage - 1) * PageSize;
            var page = await query
                .OrderBy(p => p.ProductName)
                .Skip(skip)
                .Take(PageSize)
                .ToListAsync();

            var list = new ObservableCollection<ProductRowItem>();
            var i = skip + 1;
            foreach (var p in page)
                list.Add(MapRow(i++, p));

            ProductRows = list;

            var from = TotalCount == 0 ? 0 : skip + 1;
            var to = skip + page.Count;
            PagerInfo = $"Hiển thị {from} – {to} trên tổng số {TotalCount} sản phẩm";
            CanPrevPage = CurrentPage > 1;
            CanNextPage = CurrentPage < TotalPages;
        }

        private static ProductRowItem MapRow(int index, Product p)
        {
            var name = p.ProductName;
            var initials = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var ini = initials.Length >= 2
                ? (initials[0][0].ToString() + initials[1][0]).ToUpperInvariant()
                : (name.Length >= 1 ? name[..1].ToUpperInvariant() : "?");

            var hue = (p.ProductId * 37) % 360;
            var avatarColor = new SolidColorBrush(AdminColorHelper.ColorFromHsv(hue, 0.45, 0.65));

            var culture = CultureInfo.CurrentCulture;
            string Fmt(decimal? d) => d.HasValue ? d.Value.ToString("N0", culture) : "—";

            return new ProductRowItem
            {
                Index = index,
                ProductId = p.ProductId,
                ProductName = name,
                CategoryName = p.Category?.CategoryName ?? "—",
                Unit = p.Unit ?? "—",
                CostPriceText = Fmt(p.CostPrice),
                SellingPriceText = Fmt(p.SellingPrice),
                AvatarColor = avatarColor,
                Initials = ini
            };
        }

        [RelayCommand]
        private async Task PrevPageAsync()
        {
            if (CurrentPage <= 1) return;
            CurrentPage--;
            await LoadProductsAsync();
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage >= TotalPages) return;
            CurrentPage++;
            await LoadProductsAsync();
        }

        [RelayCommand]
        private async Task AddProductAsync()
        {
            var dlg = new ProductEditWindow(_unitOfWork, null);
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true)
                await LoadProductsAsync();
        }

        [RelayCommand]
        private async Task EditProductAsync(ProductRowItem? row)
        {
            if (row == null) return;
            var product = await _unitOfWork.Products.GetByIdAsync(row.ProductId);
            if (product == null)
            {
                MessageBox.Show("Không tìm thấy sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new ProductEditWindow(_unitOfWork, product);
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true)
                await LoadProductsAsync();
        }

        [RelayCommand]
        private async Task DeleteProductAsync(ProductRowItem? row)
        {
            if (row == null) return;
            if (MessageBox.Show($"Xóa sản phẩm \"{row.ProductName}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(row.ProductId);
                if (product == null) return;

                _unitOfWork.Products.Delete(product);
                await _unitOfWork.SaveChangesAsync();
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xóa (có thể đang được dùng trong kho hoặc đơn hàng): {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
