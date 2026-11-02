
namespace EWMS.Api.Entities
{
    public class Role
    {
        public int Id { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
