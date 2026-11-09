
namespace EWMS.Api.Entities
{
    public class InboundRequestItem
    {
        public int Id { get; set; }

        public int InboundRequestId { get; set; }

        public int ProductVariantId { get; set; }

        public int ExpectedQty { get; set; }

        public InboundRequest InboundRequest { get; set; } = null!;

        public ProductVariant ProductVariant { get; set; } = null!;

        public void UpdateConfirmedDetails(int productVariantId, int expectedQty)
        {
            if (productVariantId <= 0) throw new ArgumentOutOfRangeException(nameof(productVariantId));
            if (expectedQty <= 0) throw new ArgumentOutOfRangeException(nameof(expectedQty));
            ProductVariantId = productVariantId;
            ExpectedQty = expectedQty;
        }
    }
}
