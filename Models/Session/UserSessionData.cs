namespace mvc_boilerplate.Models.Session
{
    /// <summary>
    /// User session data model for storing current user information from StaffHub
    /// </summary>
    public class UserSessionData
    {
        public string StaffId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ContactNo { get; set; }
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string DesignationId { get; set; } = string.Empty;
        public string DesignationName { get; set; } = string.Empty;
        public string? SectionId { get; set; }
        public string? SectionName { get; set; }
        public string Category { get; set; } = string.Empty;
        public DateTime DateJoined { get; set; }
        public bool IsActive { get; set; }
        public DateTime SessionCreated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Session extension methods for easy access to user data
    /// </summary>
    public static class SessionExtensions
    {
        private const string USER_SESSION_KEY = "UserSessionData";

        public static void SetUserSession(this ISession session, UserSessionData userData)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(userData);
            session.SetString(USER_SESSION_KEY, json);
        }

        public static UserSessionData? GetUserSession(this ISession session)
        {
            var json = session.GetString(USER_SESSION_KEY);
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<UserSessionData>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void ClearUserSession(this ISession session)
        {
            session.Remove(USER_SESSION_KEY);
        }

        public static bool HasUserSession(this ISession session)
        {
            return !string.IsNullOrEmpty(session.GetString(USER_SESSION_KEY));
        }
    }
}