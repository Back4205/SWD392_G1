
namespace EWMS.Api.Entities
{
    public class Stock
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }

        public int BinId { get; set; }

        public int OnHandQty { get; set; }

        public int ReservedQty { get; set; }

        public ProductVariant ProductVariant { get; set; } = null!;

        public StorageBin Bin { get; set; } = null!;
    }
}
