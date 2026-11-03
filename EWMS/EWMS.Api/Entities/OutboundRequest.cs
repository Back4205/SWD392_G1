namespace EWMS.Api.Entities
{
    public class OutboundRequest
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }

        public int RequestedByUserId { get; set; }

        public int? ApprovedByUserId { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerEmail { get; set; }

        public string? CustomerPhone { get; set; }

        public string? DestinationAddress { get; set; }

        public string? Reason { get; set; }

        public string? RejectionReason { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public Warehouse Warehouse { get; set; } = null!;

        public User RequestedByUser { get; set; } = null!;

        public User? ApprovedByUser { get; set; }

        public ICollection<OutboundRequestItem> Items { get; set; } = new List<OutboundRequestItem>();

        public ICollection<DeliveryOrder> DeliveryOrders { get; set; } = new List<DeliveryOrder>();
    }
}
