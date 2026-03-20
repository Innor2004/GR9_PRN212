namespace EWMS_WPF.BLL
{
    public class SessionInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class SessionService
    {
        private SessionInfo? _currentSession;

        public SessionInfo? CurrentSession
        {
            get => _currentSession;
            set => _currentSession = value;
        }

        public bool IsAuthenticated => _currentSession != null;

        public void ClearSession()
        {
            _currentSession = null;
        }
    }
}
