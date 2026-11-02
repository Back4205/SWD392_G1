
namespace EWMS.Api.Entities
{
    public class StorageBin
    {
        public int Id { get; set; }

        public int RackId { get; set; }

        public string BinCode { get; set; } = string.Empty;

        public string BinName { get; set; } = string.Empty;

        public decimal MaxWeightKg { get; set; }

        public StorageRack Rack { get; set; } = null!;
    }
}
