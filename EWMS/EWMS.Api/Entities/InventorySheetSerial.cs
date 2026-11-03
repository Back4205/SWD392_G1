namespace EWMS.Api.Entities
{
    public class InventorySheetSerial
    {
        public int Id { get; set; }

        public int InventorySheetId { get; set; }

        public int SerialNumberId { get; set; }

        public int? ExpectedBinId { get; set; }

        public int? ScannedBinId { get; set; }

        public bool IsExpected { get; set; }

        public bool IsScanned { get; set; }

        public string ResultStatus { get; set; } = string.Empty;

        public int? CountedByUserId { get; set; }

        public DateTime? ScannedAt { get; set; }

        public InventorySheet InventorySheet { get; set; } = null!;

        public SerialNumber SerialNumber { get; set; } = null!;

        public StorageBin? ExpectedBin { get; set; }

        public StorageBin? ScannedBin { get; set; }

        public User? CountedByUser { get; set; }
    }
}
