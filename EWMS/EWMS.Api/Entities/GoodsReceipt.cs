
namespace EWMS.Api.Entities
{
    public class GoodsReceipt
    {
        public int Id { get; set; }

        public int InboundRequestId { get; set; }

        public int ReceivedByUserId { get; set; }

        public DateTime ReceivedDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public int? ApprovedByUserId { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public string? RejectionReason { get; set; }

        public string? ApprovalNote { get; set; }

        // SQL tự tạo Version mới khi phiếu thay đổi, giúp phát hiện người dùng đang sửa bản cũ.
        public byte[] Version { get; set; } = Array.Empty<byte>();

        public DateTime UpdatedAt { get; set; }

        public List<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();

        public InboundRequest InboundRequest { get; set; } = null!;

        public User ReceivedByUser { get; set; } = null!;

        public User? ApprovedByUser { get; set; }
    }
}
