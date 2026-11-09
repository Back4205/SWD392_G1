using EWMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EWMS.Api.Tests;

public class VietnameseTextSchemaTests
{
    [Theory]
    [InlineData("User", "FullName", "nvarchar(100)")]
    [InlineData("Role", "Description", "nvarchar(255)")]
    [InlineData("Permission", "Description", "nvarchar(255)")]
    [InlineData("Category", "CategoryName", "nvarchar(100)")]
    [InlineData("Supplier", "SupplierName", "nvarchar(100)")]
    [InlineData("Supplier", "ContactPerson", "nvarchar(100)")]
    [InlineData("Supplier", "Address", "nvarchar(255)")]
    [InlineData("Product", "ProductName", "nvarchar(100)")]
    [InlineData("ProductVariant", "VariantName", "nvarchar(100)")]
    [InlineData("ProductVariant", "Specs", "nvarchar(500)")]
    [InlineData("Warehouse", "WarehouseName", "nvarchar(100)")]
    [InlineData("Warehouse", "Address", "nvarchar(255)")]
    [InlineData("StorageSection", "SectionName", "nvarchar(100)")]
    [InlineData("StorageSectionType", "Description", "nvarchar(255)")]
    [InlineData("StorageBin", "BinName", "nvarchar(100)")]
    public void HumanReadableTextUsesUnicodeStorage(string entityName, string property, string expectedType)
    {
        // Model validation only: never connects to an application or test database.
        using var context = new EWMSDbContext(new DbContextOptionsBuilder<EWMSDbContext>()
            .UseSqlServer("Server=.;Database=UnusedModelValidation;Integrated Security=true").Options);
        var entity = context.Model.GetEntityTypes().Single(x => x.ClrType.Name == entityName);
        Assert.Equal(expectedType, entity.FindProperty(property)!.GetColumnType());
    }
}
