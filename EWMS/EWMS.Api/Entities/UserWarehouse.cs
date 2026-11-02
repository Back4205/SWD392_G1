
namespace EWMS.Api.Entities
{
    public class UserWarehouse
    {
        public int UserId { get; set; }

        public int WarehouseId { get; set; }

        public User User { get; set; } = null!;

        public Warehouse Warehouse { get; set; } = null!;
    }
}
