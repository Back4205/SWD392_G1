
namespace EWMS.Api.Entities
{
    public class Warehouse
    {
        public int Id { get; set; }

        public string WarehouseCode { get; set; } = string.Empty;

        public string WarehouseName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public int ManagerUserId { get; set; }

        public decimal Area { get; set; }

        public int MaxStorageSection { get; set; }

        public decimal MaxWeightKg { get; set; }

        public string Status { get; set; } = string.Empty;

        public User ManagerUser { get; set; } = null!;
    }
}
