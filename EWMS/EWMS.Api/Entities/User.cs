
namespace EWMS.Api.Entities
{
    public class User
    {
        public int Id { get; set; }

        public int RoleId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string Status { get; set; } = string.Empty;

        public Role Role { get; set; } = null!;
    }
}
