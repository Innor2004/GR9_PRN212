using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EWMS_WPF.DAL;
using EWMS_WPF.Models;
using Microsoft.EntityFrameworkCore;

namespace EWMS_WPF.Views.Admin
{
    public partial class UserEditWindow : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly User? _existing;

        public UserEditWindow(IUnitOfWork unitOfWork, User? existing)
        {
            InitializeComponent();
            _unitOfWork = unitOfWork;
            _existing = existing;
            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            var roles = await _unitOfWork.Roles.GetQueryable().OrderBy(r => r.RoleName).ToListAsync();
            cmbRole.ItemsSource = roles;

            if (_existing != null)
            {
                Title = "Sửa người dùng";
                txtTitle.Text = "Sửa người dùng";
                txtUsername.Text = _existing.Username;
                txtUsername.IsReadOnly = true;
                txtFullName.Text = _existing.FullName ?? "";
                txtEmail.Text = _existing.Email ?? "";
                txtPhone.Text = _existing.Phone ?? "";
                chkActive.IsChecked = _existing.IsActive != false;
                cmbRole.SelectedValue = _existing.RoleId;
            }
            else
            {
                cmbRole.SelectedIndex = roles.Count > 0 ? 0 : -1;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUsername.Text?.Trim() ?? "";
            var pwd = txtPassword.Password;
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Nhập tên đăng nhập.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbRole.SelectedValue is not int roleId)
            {
                MessageBox.Show("Chọn vai trò.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_existing == null && string.IsNullOrEmpty(pwd))
            {
                MessageBox.Show("Nhập mật khẩu cho tài khoản mới.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_existing == null)
                {
                    var exists = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username);
                    if (exists != null)
                    {
                        MessageBox.Show("Tên đăng nhập đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var u = new User
                    {
                        Username = username,
                        PasswordHash = pwd,
                        FullName = string.IsNullOrWhiteSpace(txtFullName.Text) ? null : txtFullName.Text.Trim(),
                        Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                        Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim(),
                        RoleId = roleId,
                        IsActive = chkActive.IsChecked == true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Users.AddAsync(u);
                }
                else
                {
                    _existing.FullName = string.IsNullOrWhiteSpace(txtFullName.Text) ? null : txtFullName.Text.Trim();
                    _existing.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
                    _existing.Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim();
                    _existing.RoleId = roleId;
                    _existing.IsActive = chkActive.IsChecked == true;
                    _existing.UpdatedAt = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(pwd))
                        _existing.PasswordHash = pwd;
                    _unitOfWork.Users.Update(_existing);
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
