
namespace EWMS.Api.DTOs.InboundRequests
{
    public class InboundRequestDetailDto
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }

        public int? SupplierId { get; set; }

        public int RequestedByUserId { get; set; }

        public int? AssignedStaffUserId { get; set; }

        public string? AssignedStaffName { get; set; }

        public string Revision { get; set; } = string.Empty;

        public int? ApprovedByUserId { get; set; }

        public string? DecidedByName { get; set; }

        public DateTime? DecisionDate { get; set; }

        public string? SupplierCommitmentStatus { get; set; }

        public bool HasGoodsReceipts { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? RejectionReason { get; set; }

        public string? ApprovalNote { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public List<InboundRequestDetailItemDto> Items { get; set; }
            = new();
    }

    public class InboundRequestDetailItemDto
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }

        public int ExpectedQty { get; set; }

        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
    }
}
