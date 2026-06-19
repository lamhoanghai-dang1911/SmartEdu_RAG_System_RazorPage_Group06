using SmartEdu.Shared.Enums;

namespace SmartEdu.DataAccess.EntityModels
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string Email { get; set; } = string.Empty;
        public string? StudentCode { get; set; }
        public bool RequirePasswordChange { get; set; } = false;
    }
}
