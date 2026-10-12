namespace mvc_boilerplate.Models.UI
{
    public class StaffHubAuthInfo
    {
        public string AuthMode { get; set; } = string.Empty;
        public string? AzureRedirectUrl { get; set; }
        public string? LocalLoginUrl { get; set; }
    }

    public class AzureRedirectResponse
    {
        public string RedirectUrl { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class SessionLoginResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserProfile? Profile { get; set; }
    }

    public class UserProfile
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;
    }
}