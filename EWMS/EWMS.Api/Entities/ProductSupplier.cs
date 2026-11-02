
namespace EWMS.Api.Entities
{
    public class ProductSupplier
    {
        public int ProductId { get; set; }

        public int SupplierId { get; set; }

        public string? SupplierSKU { get; set; }

        public decimal? UnitPrice { get; set; }

        public int? LeadTimeDays { get; set; }

        public int IsPreferred { get; set; }

        public Product Product { get; set; } = null!;

        public Supplier Supplier { get; set; } = null!;
    }
}
