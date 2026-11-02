
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

        public InboundRequest InboundRequest { get; set; } = null!;

        public User ReceivedByUser { get; set; } = null!;

        public User? ApprovedByUser { get; set; }
    }
}
