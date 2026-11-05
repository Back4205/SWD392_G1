
namespace EWMS.Api.Entities
{
    public class GoodsReceiptItem
    {
        public int Id { get; set; }

        public int GoodsReceiptId { get; set; }

        public int ProductVariantId { get; set; }

        // Khi còn nháp, nhân viên được quét serial trước rồi chọn bin sau.
        public int? BinId { get; set; }

        public int ReceivedQty { get; set; }

        public GoodsReceipt GoodsReceipt { get; set; } = null!;

        public ProductVariant ProductVariant { get; set; } = null!;

        public StorageBin? Bin { get; set; }

        public List<GoodsReceiptSerial> Serials { get; set; } = new List<GoodsReceiptSerial>();
    }
}
