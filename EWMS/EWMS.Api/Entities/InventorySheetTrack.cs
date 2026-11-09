namespace EWMS.Api.Entities
{
    public class InventorySheetTrack
    {
        public int Id { get; set; }

        public int InventorySheetItemId { get; set; }

        public int CountedByUserId { get; set; }

        public int CountedQty { get; set; }

        public string? Note { get; set; }

        public DateTime CountedAt { get; set; }

        public InventorySheetItem InventorySheetItem { get; set; } = null!;

        public User CountedByUser { get; set; } = null!;
    }
}
