using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EWMS_WPF.DAL;
using EWMS_WPF.Models;
using Microsoft.EntityFrameworkCore;

namespace EWMS_WPF.Views.Admin
{
    public partial class ProductEditWindow : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Product? _existing;

        public ProductEditWindow(IUnitOfWork unitOfWork, Product? existing)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _existing = existing;
            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            var cats = await _unitOfWork.ProductCategories.GetQueryable()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var withEmpty = new System.Collections.Generic.List<ProductCategory>
            {
                new ProductCategory { CategoryId = 0, CategoryName = "(Không chọn)" }
            };
            withEmpty.AddRange(cats);
            cmbCategory.ItemsSource = withEmpty;

            if (_existing != null)
            {
                Title = "Sửa sản phẩm";
                txtTitle.Text = "Sửa sản phẩm";
                txtName.Text = _existing.ProductName;
                txtUnit.Text = _existing.Unit ?? "";
                txtCost.Text = _existing.CostPrice?.ToString(CultureInfo.CurrentCulture) ?? "";
                txtSell.Text = _existing.SellingPrice?.ToString(CultureInfo.CurrentCulture) ?? "";
                cmbCategory.SelectedValue = _existing.CategoryId ?? 0;
            }
            else
            {
                cmbCategory.SelectedValue = 0;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var name = txtName.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Nhập tên sản phẩm.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? categoryId = cmbCategory.SelectedValue is int cid && cid > 0 ? cid : null;

            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(txtCost.Text))
            {
                if (!decimal.TryParse(txtCost.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var c))
                {
                    MessageBox.Show("Giá vốn không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                cost = c;
            }

            decimal? sell = null;
            if (!string.IsNullOrWhiteSpace(txtSell.Text))
            {
                if (!decimal.TryParse(txtSell.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var s))
                {
                    MessageBox.Show("Giá bán không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                sell = s;
            }

            try
            {
                if (_existing == null)
                {
                    var dup = await _unitOfWork.Products.FirstOrDefaultAsync(p => p.ProductName == name);
                    if (dup != null)
                    {
                        MessageBox.Show("Đã có sản phẩm cùng tên.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var p = new Product
                    {
                        ProductName = name,
                        CategoryId = categoryId,
                        Unit = string.IsNullOrWhiteSpace(txtUnit.Text) ? null : txtUnit.Text.Trim(),
                        CostPrice = cost,
                        SellingPrice = sell
                    };
                    await _unitOfWork.Products.AddAsync(p);
                }
                else
                {
                    _existing.ProductName = name;
                    _existing.CategoryId = categoryId;
                    _existing.Unit = string.IsNullOrWhiteSpace(txtUnit.Text) ? null : txtUnit.Text.Trim();
                    _existing.CostPrice = cost;
                    _existing.SellingPrice = sell;
                    _unitOfWork.Products.Update(_existing);
                }

                await _unitOfWork.SaveChangesAsync();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lưu thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
