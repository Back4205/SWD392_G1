namespace EWMS.Api.Entities
{
    public class DeliveryOrderItem
    {
        public int Id { get; set; }

        public int DeliveryOrderId { get; set; }

        public int ProductVariantId { get; set; }

        public int BinId { get; set; }

        public int DispatchedQty { get; set; }

        public DeliveryOrder DeliveryOrder { get; set; } = null!;

        public ProductVariant ProductVariant { get; set; } = null!;

        public StorageBin Bin { get; set; } = null!;

        public ICollection<SerialNumber> DispatchedSerialNumbers { get; set; } = new List<SerialNumber>();
    }
}
