
namespace EWMS.Api.Entities
{
    public class Supplier
    {
        public int Id { get; set; }

        public string SupplierCode { get; set; } = string.Empty;

        public string SupplierName { get; set; } = string.Empty;

        public string ContactPerson { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
