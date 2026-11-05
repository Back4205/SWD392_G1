
namespace EWMS.Api.Entities
{
    public class InboundRequest
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }

        public int? SupplierId { get; set; }

        public int RequestedByUserId { get; set; }

        public int? ApprovedByUserId { get; set; }

        public string? SupplierCommitmentStatus { get; set; }

        // Bảng kê dự kiến do Purchasing xác nhận; không phải hàng đã nhập kho.
        public bool HasSupplierManifest { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? RejectionReason { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public Warehouse Warehouse { get; set; } = null!;

        public Supplier? Supplier { get; set; }

        public User RequestedByUser { get; set; } = null!;

        public User? ApprovedByUser { get; set; }
    }
}
