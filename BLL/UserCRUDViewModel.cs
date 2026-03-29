using System;
using System.Collections.ObjectModel;
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
    public class UserRowItem
    {
        public int Index { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Role { get; set; } = "";
        public Brush RoleBg { get; set; } = Brushes.Transparent;
        public Brush RoleColor { get; set; } = Brushes.White;
        public DateTime? CreatedAt { get; set; }
        public string StatusText { get; set; } = "";
        public Brush StatusColor { get; set; } = Brushes.Gray;
        public Brush AvatarColor { get; set; } = Brushes.Gray;
        public string Initials { get; set; } = "?";
    }

    public partial class UserCRUDViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;

        private const int PageSize = 10;

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private int _roleFilterIndex;

        [ObservableProperty]
        private int _statusFilterIndex;

        [ObservableProperty]
        private ObservableCollection<UserRowItem> _userRows = new();

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

        public UserCRUDViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task InitializeAsync()
        {
            await LoadUsersAsync();
        }

        partial void OnSearchTextChanged(string value) => _ = LoadUsersAsync();
        partial void OnRoleFilterIndexChanged(int value) => _ = LoadUsersAsync();
        partial void OnStatusFilterIndexChanged(int value) => _ = LoadUsersAsync();

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            var query = _unitOfWork.Users.GetQueryable()
                .Include(u => u.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var t = SearchText.Trim();
                query = query.Where(u =>
                    (u.Username != null && u.Username.Contains(t)) ||
                    (u.FullName != null && u.FullName.Contains(t)) ||
                    (u.Email != null && u.Email.Contains(t)));
            }

            if (RoleFilterIndex > 0)
            {
                var labels = new[] { "", "Admin", "Manager", "Staff", "Viewer" };
                if (RoleFilterIndex < labels.Length)
                {
                    var key = labels[RoleFilterIndex];
                    query = query.Where(u => u.Role != null && u.Role.RoleName.Contains(key));
                }
            }

            if (StatusFilterIndex == 1)
                query = query.Where(u => u.IsActive == true);
            else if (StatusFilterIndex == 2)
                query = query.Where(u => u.IsActive == false);

            TotalCount = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;
            if (CurrentPage < 1)
                CurrentPage = 1;

            var skip = (CurrentPage - 1) * PageSize;
            var page = await query
                .OrderBy(u => u.UserId)
                .Skip(skip)
                .Take(PageSize)
                .ToListAsync();

            var list = new ObservableCollection<UserRowItem>();
            var i = skip + 1;
            foreach (var u in page)
            {
                list.Add(MapRow(i++, u));
            }

            UserRows = list;

            var from = TotalCount == 0 ? 0 : skip + 1;
            var to = skip + page.Count;
            PagerInfo = $"Hiển thị {from} – {to} trên tổng số {TotalCount} người dùng";
            CanPrevPage = CurrentPage > 1;
            CanNextPage = CurrentPage < TotalPages;
        }

        private static UserRowItem MapRow(int index, User u)
        {
            var name = u.FullName ?? u.Username ?? "?";
            var initials = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var ini = initials.Length >= 2
                ? (initials[0][0].ToString() + initials[^1][0]).ToUpperInvariant()
                : (name.Length >= 1 ? name[..1].ToUpperInvariant() : "?");

            var hue = (u.UserId * 47) % 360;
            var avatarColor = new SolidColorBrush(AdminColorHelper.ColorFromHsv(hue, 0.45, 0.65));

            var roleName = u.Role?.RoleName ?? "";
            var badge = RoleBadgeColors(roleName);

            var active = u.IsActive == true;
            return new UserRowItem
            {
                Index = index,
                UserId = u.UserId,
                FullName = name,
                Email = u.Email ?? "",
                Phone = u.Phone ?? "",
                Role = roleName,
                RoleBg = new SolidColorBrush(badge.bg),
                RoleColor = new SolidColorBrush(badge.fg),
                CreatedAt = u.CreatedAt,
                StatusText = active ? "Hoạt động" : "Khóa",
                StatusColor = active ? new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99)) : new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)),
                AvatarColor = avatarColor,
                Initials = ini
            };
        }

        private static (Color bg, Color fg) RoleBadgeColors(string roleName)
        {
            var r = roleName.ToLowerInvariant();
            if (r.Contains("admin"))
                return (Color.FromRgb(0x2A, 0x1F, 0x3D), Color.FromRgb(0xA7, 0x8B, 0xFA));
            if (r.Contains("manager"))
                return (Color.FromRgb(0x1E, 0x3A, 0x5F), Color.FromRgb(0x4F, 0x8E, 0xF7));
            if (r.Contains("sales"))
                return (Color.FromRgb(0x1A, 0x3D, 0x2E), Color.FromRgb(0x34, 0xD3, 0x99));
            if (r.Contains("inventory"))
                return (Color.FromRgb(0x3D, 0x2E, 0x1A), Color.FromRgb(0xFB, 0xBF, 0x24));
            return (Color.FromRgb(0x22, 0x26, 0x3A), Color.FromRgb(0x8A, 0x93, 0xB2));
        }

        [RelayCommand]
        private async Task PrevPageAsync()
        {
            if (CurrentPage <= 1) return;
            CurrentPage--;
            await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage >= TotalPages) return;
            CurrentPage++;
            await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task AddUserAsync()
        {
            var dlg = new UserEditWindow(_unitOfWork, null);
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true)
                await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task EditUserAsync(UserRowItem? row)
        {
            if (row == null) return;
            var user = await _unitOfWork.Users.GetByIdAsync(row.UserId);
            if (user == null)
            {
                MessageBox.Show("Không tìm thấy người dùng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new UserEditWindow(_unitOfWork, user);
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true)
                await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task DeleteUserAsync(UserRowItem? row)
        {
            if (row == null) return;
            if (MessageBox.Show($"Xóa người dùng \"{row.FullName}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(row.UserId);
                if (user == null) return;

                var links = await _unitOfWork.UserWarehouses.FindAsync(uw => uw.UserId == user.UserId);
                foreach (var uw in links)
                    _unitOfWork.UserWarehouses.Delete(uw);

                _unitOfWork.Users.Delete(user);
                await _unitOfWork.SaveChangesAsync();
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
