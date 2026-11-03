namespace EWMS.Api.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
