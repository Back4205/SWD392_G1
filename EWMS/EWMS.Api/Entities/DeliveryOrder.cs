namespace EWMS.Api.Entities
{
    public class DeliveryOrder
    {
        public int Id { get; set; }

        public int OutboundRequestId { get; set; }

        public int CreatedByUserId { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public OutboundRequest OutboundRequest { get; set; } = null!;

        public User CreatedByUser { get; set; } = null!;

        public ICollection<DeliveryOrderItem> Items { get; set; } = new List<DeliveryOrderItem>();
    }
}
