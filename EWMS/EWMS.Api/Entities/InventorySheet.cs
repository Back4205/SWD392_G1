namespace EWMS.Api.Entities
{
    public class InventorySheet
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }

        public int CreatedByUserId { get; set; }

        public int? ApprovedByUserId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? RejectionReason { get; set; }

        public DateTime CreatedDate { get; set; }

        public Warehouse Warehouse { get; set; } = null!;

        public User CreatedByUser { get; set; } = null!;

        public User? ApprovedByUser { get; set; }

        public ICollection<InventorySheetItem> Items { get; set; } = new List<InventorySheetItem>();

        public ICollection<InventorySheetSerial> Serials { get; set; } = new List<InventorySheetSerial>();
    }
}
