
namespace EWMS.Api.Entities
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int? ProductId { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string? VariantName { get; set; }

        public string? Color { get; set; }

        public string? Capacity { get; set; }

        public decimal WeightKg { get; set; }

        public decimal CostPrice { get; set; }

        public decimal SellingPrice { get; set; }

        public int? WarrantyMonths { get; set; }

        public string? Specs { get; set; }

        public Product? Product { get; set; }
    }
}
