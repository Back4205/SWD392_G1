using Microsoft.AspNetCore.Identity;
using EWMS.Api.Data;
using EWMS.Api.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EWMS.Api.Tests;

// Mỗi lần chạy dùng database mới có GUID; không đọc cấu hình database ứng dụng.
public class SqlServerFixture : IAsyncLifetime
{
    public const string TestPassword = "Test-Only-Password-42!";
    private const string Prefix = "EWMS_GoodsReceiptTests_";
    private readonly string _databaseName = Prefix + Guid.NewGuid().ToString("N");
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("EWMS_TEST_SQL_CONNECTION")
            ?? "Server=.;Integrated Security=true;TrustServerCertificate=true;Encrypt=false")
        {
            InitialCatalog = _databaseName,
            ConnectTimeout = 10
        };
        ConnectionString = connection.ConnectionString;
        using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public EWMSDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EWMSDbContext>()
            .UseSqlServer(ConnectionString).Options;
        return new EWMSDbContext(options);
    }

    public async Task DisposeAsync()
    {
        var target = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        // Chỉ xóa đúng database fixture vừa tạo, không chấp nhận tên từ ứng dụng.
        if (target != _databaseName || !target.StartsWith(Prefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(target[Prefix.Length..], "N", out _))
        {
            throw new InvalidOperationException("Refusing to remove a non-test database.");
        }
        using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    public async Task<TestData> SeedAsync(int expected = 10, decimal capacity = 1000)
    {
        using var context = CreateContext();
        string suffix = Guid.NewGuid().ToString("N")[..12];
        async Task<Role> RoleAsync(string name)
        {
            var role = await context.Roles.SingleOrDefaultAsync(x => x.RoleName == name);
            if (role == null)
            {
                role = new Role { RoleName = name };
                context.Roles.Add(role);
                await context.SaveChangesAsync();
            }
            return role;
        }
        var staffRole = await RoleAsync("Warehouse Staff");
        var managerRole = await RoleAsync("Manager");
        var purchasingRole = await RoleAsync("Purchasing Manager");
        User NewUser(string label, Role role, string status = "Active")
        {
            var user = new User
            {
                UserName = label + suffix, FullName = label,
                Email = label + suffix + "@example.test", PasswordHash = "TEST-NOT-A-LOGIN-HASH",
                RoleId = role.Id, Status = status
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, TestPassword);
            return user;
        }
        var staff = NewUser("staff", staffRole);
        var manager = NewUser("manager", managerRole);
        var other = NewUser("other", staffRole);
        var inactive = NewUser("inactive", staffRole, "Inactive");
        var purchasing = NewUser("purchasing", purchasingRole);
        context.Users.AddRange(staff, manager, other, inactive, purchasing);
        await context.SaveChangesAsync();
        Warehouse NewWarehouse(string code)
        {
            return new Warehouse
            {
                WarehouseCode = code + suffix, WarehouseName = code, Address = "Test fixture",
                ManagerUserId = manager.Id, Status = "Active", Area = 100,
                MaxStorageSection = 10, MaxWeightKg = capacity
            };
        }
        var warehouse = NewWarehouse("A-");
        var otherWarehouse = NewWarehouse("B-");
        var type = new StorageSectionType { TypeCode = "T-" + suffix };
        var category = new Category { CategoryName = "C-" + suffix };
        context.AddRange(warehouse, otherWarehouse, type, category);
        await context.SaveChangesAsync();
        foreach (var user in new[] { staff, manager, inactive, purchasing })
            context.UserWarehouses.Add(new UserWarehouse { UserId = user.Id, WarehouseId = warehouse.Id });
        context.UserWarehouses.Add(new UserWarehouse { UserId = other.Id, WarehouseId = otherWarehouse.Id });
        var section = new StorageSection
        {
            WarehouseId = warehouse.Id, StorageSectionTypeId = type.Id,
            SectionCode = "S-" + suffix, SectionName = "Test", MaxRackCount = 10
        };
        var rack = new StorageRack { Section = section, RackCode = "R-" + suffix, MaxBinCount = 10, MaxWeightKg = capacity };
        var bin = new StorageBin { Rack = rack, BinCode = "BIN-" + suffix, BinName = "Test", MaxWeightKg = capacity };
        var bin2 = new StorageBin { Rack = rack, BinCode = "BIN2-" + suffix, BinName = "Test2", MaxWeightKg = capacity };
        var otherSection = new StorageSection
        {
            WarehouseId = otherWarehouse.Id, StorageSectionTypeId = type.Id,
            SectionCode = "SO-" + suffix, SectionName = "Other", MaxRackCount = 10
        };
        var otherRack = new StorageRack { Section = otherSection, RackCode = "RO-" + suffix, MaxBinCount = 10, MaxWeightKg = capacity };
        var otherBin = new StorageBin { Rack = otherRack, BinCode = "BO-" + suffix, BinName = "Other", MaxWeightKg = capacity };
        var product = new Product
        {
            CategoryId = category.Id, ProductCode = "P-" + suffix, ProductName = "Device",
            Brand = "Test", Model = "Test", Status = "Active"
        };
        var variant = new ProductVariant { Product = product, SKU = "SKU-" + suffix, WeightKg = 2, CostPrice = 10, SellingPrice = 20 };
        var variant2 = new ProductVariant { Product = product, SKU = "SKU2-" + suffix, WeightKg = 3, CostPrice = 10, SellingPrice = 20 };
        var inbound = new InboundRequest
        {
            WarehouseId = warehouse.Id, RequestedByUserId = purchasing.Id, ApprovedByUserId = manager.Id,
            Status = "Approved", SupplierCommitmentStatus = "Confirmed", CreatedDate = DateTime.UtcNow
        };
        context.AddRange(bin, bin2, otherBin, variant, variant2, inbound);
        await context.SaveChangesAsync();
        context.InboundRequestItems.Add(new InboundRequestItem
        {
            InboundRequestId = inbound.Id, ProductVariantId = variant.Id, ExpectedQty = expected
        });
        await context.SaveChangesAsync();
        return new TestData
        {
            Suffix = suffix, StaffId = staff.Id, ManagerId = manager.Id, OtherUserId = other.Id,
            InactiveUserId = inactive.Id, PurchasingUserId = purchasing.Id, WarehouseId = warehouse.Id,
            OtherWarehouseId = otherWarehouse.Id, RackId = rack.Id, BinId = bin.Id, SecondBinId = bin2.Id,
            OtherBinId = otherBin.Id, VariantId = variant.Id, SecondVariantId = variant2.Id,
            SKU = variant.SKU, InboundId = inbound.Id
        };
    }
}

public class TestData
{
    public string Suffix { get; set; } = string.Empty;
    public int StaffId { get; set; }
    public int ManagerId { get; set; }
    public int OtherUserId { get; set; }
    public int InactiveUserId { get; set; }
    public int PurchasingUserId { get; set; }
    public int WarehouseId { get; set; }
    public int OtherWarehouseId { get; set; }
    public int RackId { get; set; }
    public int BinId { get; set; }
    public int SecondBinId { get; set; }
    public int OtherBinId { get; set; }
    public int VariantId { get; set; }
    public int SecondVariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int InboundId { get; set; }
}

[CollectionDefinition("Receiving SQL", DisableParallelization = true)]
public class ReceivingSqlCollection : ICollectionFixture<SqlServerFixture> { }
