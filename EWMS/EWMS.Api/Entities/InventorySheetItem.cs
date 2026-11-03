namespace EWMS.Api.Entities
{
    public class InventorySheetItem
    {
        public int Id { get; set; }

        public int InventorySheetId { get; set; }

        public int BinId { get; set; }

        public int ProductVariantId { get; set; }

        public int SystemQty { get; set; }

        public int? ActualQty { get; set; }

        public string? Note { get; set; }

        public InventorySheet InventorySheet { get; set; } = null!;

        public StorageBin Bin { get; set; } = null!;

        public ProductVariant ProductVariant { get; set; } = null!;

        public ICollection<InventorySheetTrack> Tracks { get; set; } = new List<InventorySheetTrack>();
    }
}
