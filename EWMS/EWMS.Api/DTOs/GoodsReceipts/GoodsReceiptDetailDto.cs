namespace EWMS.Api.DTOs.GoodsReceipts
{
    public class GoodsReceiptDetailDto
    {
        public int Id { get; set; }
        public int InboundRequestId { get; set; }
        public int WarehouseId { get; set; }
        public int ReceivedByUserId { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int? DecidedByUserId { get; set; }
        public string? DecidedByName { get; set; }
        public DateTime? DecisionDate { get; set; }
        public string? RejectionReason { get; set; }
        public string? ApprovalNote { get; set; }
        public List<GoodsReceiptItemDto> Items { get; set; } = new List<GoodsReceiptItemDto>();
    }

    public class GoodsReceiptItemDto
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int? BinId { get; set; }
        public int ReceivedQty { get; set; }
        public int ExpectedQty { get; set; }
        public int ApprovedReceivedQty { get; set; }
        public int RemainingQty { get; set; }
        public List<ReceiptSerialDto> Serials { get; set; } = new List<ReceiptSerialDto>();
    }

    public class ReceiptSerialDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string SerialCode { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; }
        public int ReceivedQty { get; set; }
        public string Version { get; set; } = string.Empty;
    }
}
