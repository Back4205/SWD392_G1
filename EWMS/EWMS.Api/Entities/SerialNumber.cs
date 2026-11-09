
namespace EWMS.Api.Entities
{
    public class SerialNumber
    {
        public void UpdateStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Serial status is required.", nameof(status));
            CurrentStatus = status;
        }

        public int Id { get; set; }

        public int ProductVariantId { get; set; }

        public int? OriginGoodsReceiptItemId { get; set; }

        public int? ReservedForOutboundItemId { get; set; }

        public int? CurrentBinId { get; set; }

        public int? DispatchedInDeliveryItemId { get; set; }

        public string SerialCode { get; set; } = string.Empty;

        public string CurrentStatus { get; set; } = string.Empty;

        public ProductVariant ProductVariant { get; set; } = null!;

        public GoodsReceiptItem? OriginGoodsReceiptItem { get; set; }

        public StorageBin? CurrentBin { get; set; }

        public OutboundRequestItem? ReservedForOutboundItem { get; set; }

        public DeliveryOrderItem? DispatchedInDeliveryItem { get; set; }
    }
}
