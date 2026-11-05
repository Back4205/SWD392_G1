
using System.Collections.Generic;

namespace EWMS.Api.DTOs.InboundRequests
{
    public class InboundReceivingProgressDto
    {
        public int InboundRequestId { get; set; }

        public int TotalExpectedQty { get; set; }

        // Giữ kiểu nullable để tương thích contract cũ; service hiện trả số tính từ receipt Approved.
        public int? TotalReceivedQty { get; set; }

        public int? RemainingQty { get; set; }

        public double? ProgressPercentage { get; set; }

        public string ProgressStatus { get; set; } = "NotAvailable";

        public List<InboundReceivingProgressItemDto> Items { get; set; }
            = new();
    }

    public class InboundReceivingProgressItemDto
    {
        public int ProductVariantId { get; set; }

        public int ExpectedQty { get; set; }

        public int? ReceivedQty { get; set; }

        public int? RemainingQty { get; set; }
    }
}
