using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EWMS_WPF.DAL;
using Microsoft.EntityFrameworkCore;

namespace EWMS_WPF.BLL
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public LoginViewModel(IUnitOfWork unitOfWork, SessionService sessionService)
        {
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return;
            }

            IsLoading = true;

            try
            {
                var user = await _unitOfWork.Users.GetQueryable()
                    .Include(u => u.Role)
                    .Include(u => u.UserWarehouses)
                        .ThenInclude(uw => uw.Warehouse)
                    .FirstOrDefaultAsync(u => u.Username == Username && u.PasswordHash == Password);

                if (user == null || user.IsActive == false)
                {
                    ErrorMessage = "Invalid username or password, or account is inactive.";
                    return;
                }

                var userWarehouse = user.UserWarehouses.FirstOrDefault();

                _sessionService.CurrentSession = new SessionInfo
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName ?? user.Username,
                    RoleName = user.Role?.RoleName ?? string.Empty,
                    WarehouseId = userWarehouse?.WarehouseId ?? 0,
                    WarehouseName = userWarehouse?.Warehouse?.WarehouseName ?? "No Warehouse"
                };

                // Success - MainWindow sẽ mở từ View
                OnLoginSuccess?.Invoke();
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public System.Action? OnLoginSuccess { get; set; }
    }
}
