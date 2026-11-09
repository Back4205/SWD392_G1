namespace EWMS.Api.Entities
{
    public class OutboundRequestItem
    {
        public int Id { get; set; }

        public int OutboundRequestId { get; set; }

        public int ProductVariantId { get; set; }

        public int RequestedQty { get; set; }

        public OutboundRequest OutboundRequest { get; set; } = null!;

        public ProductVariant ProductVariant { get; set; } = null!;

        public ICollection<SerialNumber> ReservedSerialNumbers { get; set; } = new List<SerialNumber>();
    }
}
