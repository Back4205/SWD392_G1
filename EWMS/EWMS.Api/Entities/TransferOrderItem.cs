namespace EWMS.Api.Entities
{
    public class TransferOrderItem
    {
        public int Id { get; set; }

        public int TransferOrderId { get; set; }

        public int ProductVariantId { get; set; }

        public int FromBinId { get; set; }

        public int ToBinId { get; set; }

        public int RequestedQty { get; set; }

        public int? ActualReceivedQty { get; set; }

        public string? DiscrepancyNote { get; set; }

        public TransferOrder TransferOrder { get; set; } = null!;

        public ProductVariant ProductVariant { get; set; } = null!;

        public StorageBin FromBin { get; set; } = null!;

        public StorageBin ToBin { get; set; } = null!;

        public ICollection<TransferOrderSerial> Serials { get; set; } = new List<TransferOrderSerial>();
    }
}
