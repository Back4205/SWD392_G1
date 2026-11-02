
namespace EWMS.Api.Entities
{
    public class SerialNumber
    {
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
    }
}
