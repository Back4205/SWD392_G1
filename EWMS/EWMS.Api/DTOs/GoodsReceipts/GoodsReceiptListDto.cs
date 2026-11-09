using System.ComponentModel.DataAnnotations;

namespace EWMS.Api.DTOs.GoodsReceipts
{
    public class GoodsReceiptFilterDto
    {
        public string? Status { get; set; }
        public int? WarehouseId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
        [Range(1, 100)] public int PageSize { get; set; } = 20;
    }

    public class GoodsReceiptListDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<GoodsReceiptSummaryDto> Items { get; set; } = new List<GoodsReceiptSummaryDto>();
    }

    public class GoodsReceiptSummaryDto
    {
        public int Id { get; set; }
        public int InboundRequestId { get; set; }
        public int WarehouseId { get; set; }
        public int ReceivedByUserId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }

    public class ReceivingLineDto
    {
        public int ProductVariantId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int ExpectedQty { get; set; }
        public int ReceivedQty { get; set; }
        public int RemainingQty { get; set; }
    }
}
