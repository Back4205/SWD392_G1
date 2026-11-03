namespace EWMS.Api.Entities
{
    public class TransferOrder
    {
        public int Id { get; set; }

        public int FromWarehouseId { get; set; }

        public int ToWarehouseId { get; set; }

        public int RequestedByUserId { get; set; }

        public int? ApprovedByUserId { get; set; }

        public int? DestinationStaffUserId { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public Warehouse FromWarehouse { get; set; } = null!;

        public Warehouse ToWarehouse { get; set; } = null!;

        public User RequestedByUser { get; set; } = null!;

        public User? ApprovedByUser { get; set; }

        public User? DestinationStaffUser { get; set; }

        public ICollection<TransferOrderItem> Items { get; set; } = new List<TransferOrderItem>();
    }
}
