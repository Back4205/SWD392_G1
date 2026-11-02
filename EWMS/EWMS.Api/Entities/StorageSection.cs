
namespace EWMS.Api.Entities
{
    public class StorageSection
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }

        public int StorageSectionTypeId { get; set; }

        public string SectionCode { get; set; } = string.Empty;

        public string SectionName { get; set; } = string.Empty;

        public int MaxRackCount { get; set; }

        public Warehouse Warehouse { get; set; } = null!;

        public StorageSectionType StorageSectionType { get; set; } = null!;
    }
}
