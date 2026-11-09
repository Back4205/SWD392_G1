namespace EWMS.Api.Entities
{
    // Bảng scan tạm. SerialNumber chính thức chỉ được tạo khi Manager duyệt.
    public class GoodsReceiptSerial
    {
        public int Id { get; set; }
        public int GoodsReceiptId { get; set; }
        public int GoodsReceiptItemId { get; set; }
        public string SerialCode { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; }
        public int ScannedByUserId { get; set; }
        public GoodsReceiptItem GoodsReceiptItem { get; set; } = null!;
        public User ScannedByUser { get; set; } = null!;
    }
}
