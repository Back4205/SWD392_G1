
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
    }
}
