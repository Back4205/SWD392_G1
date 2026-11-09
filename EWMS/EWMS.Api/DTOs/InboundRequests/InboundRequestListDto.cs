

namespace EWMS.Api.DTOs.InboundRequests
{
    public class InboundRequestListDto
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }

        public int RequestedByUserId { get; set; }

        public int? SupplierId { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }
    }
}
