
namespace EWMS.Api.Entities
{
    public class Stock
    {
        public void IncreaseOnHand(int quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            OnHandQty = checked(OnHandQty + quantity);
        }

        public int Id { get; set; }

        public int ProductVariantId { get; set; }

        public int BinId { get; set; }

        public int OnHandQty { get; set; }

        public int ReservedQty { get; set; }

        public ProductVariant ProductVariant { get; set; } = null!;

        public StorageBin Bin { get; set; } = null!;
    }
}
