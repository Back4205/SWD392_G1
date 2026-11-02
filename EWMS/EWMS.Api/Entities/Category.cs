
namespace EWMS.Api.Entities
{
    public class Category
    {
        public int Id { get; set; }

        public int? ParentId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public Category? Parent { get; set; }
    }
}
