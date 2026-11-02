
namespace EWMS.Api.Entities
{
    public class StorageRack
    {
        public int Id { get; set; }

        public int SectionId { get; set; }

        public string RackCode { get; set; } = string.Empty;

        public int MaxBinCount { get; set; }

        public decimal MaxWeightKg { get; set; }

        public decimal? LengthCm { get; set; }

        public decimal? WidthCm { get; set; }

        public decimal? HeightCm { get; set; }

        public StorageSection Section { get; set; } = null!;
    }
}
