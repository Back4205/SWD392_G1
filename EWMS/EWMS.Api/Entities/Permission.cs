
namespace EWMS.Api.Entities
{
    public class Permission
    {
        public int Id { get; set; }

        public string PermissionName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
