using EWMS.Api.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Data
{
    // Chỉ tạo dữ liệu demo trong database riêng. Không seed vào database của nhóm.
    public static class GoodsReceiptDemoSeeder
    {
        public static async Task SeedAsync(EWMSDbContext context, IHostEnvironment environment, ILogger logger)
        {
            string databaseName = new SqlConnectionStringBuilder(context.Database.GetConnectionString()).InitialCatalog;
            if (!environment.IsDevelopment() || databaseName != "EWMS_GoodsReceipt_Development")
            {
                throw new InvalidOperationException("Demo seed requires Development and database EWMS_GoodsReceipt_Development.");
            }
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                throw new InvalidOperationException("Run dotnet ef database update for the demo database before seeding.");
            }
            using var transaction = await context.Database.BeginTransactionAsync();
            if (!await context.Warehouses.AnyAsync(x => x.WarehouseCode == "GR-DEMO-A"))
            {
                // 1. Tạo role/user trước vì Warehouse cần ManagerUserId hợp lệ.
                var managerRole = await GetRoleAsync(context, "Manager");
                var staffRole = await GetRoleAsync(context, "Warehouse Staff");
                var purchasingRole = await GetRoleAsync(context, "Purchasing Manager");
                var manager = CreateUser("gr-demo-manager", managerRole.Id);
                var staff = CreateUser("gr-demo-staff", staffRole.Id);
                var otherStaff = CreateUser("gr-demo-other-staff", staffRole.Id);
                var inactive = CreateUser("gr-demo-inactive", staffRole.Id);
                inactive.Status = "Inactive";
                var purchasing = CreateUser("gr-demo-purchasing", purchasingRole.Id);
                context.Users.AddRange(manager, staff, otherStaff, inactive, purchasing);
                await context.SaveChangesAsync();

                var warehouseA = CreateWarehouse("GR-DEMO-A", manager.Id);
                var warehouseB = CreateWarehouse("GR-DEMO-B", manager.Id);
                context.Warehouses.AddRange(warehouseA, warehouseB);
                await context.SaveChangesAsync();
                foreach (var user in new[] { manager, staff, inactive, purchasing })
                {
                    context.UserWarehouses.Add(new UserWarehouse { UserId = user.Id, WarehouseId = warehouseA.Id });
                }
                context.UserWarehouses.Add(new UserWarehouse { UserId = otherStaff.Id, WarehouseId = warehouseB.Id });

                // 2. Vị trí thật có tải trọng đủ cho kịch bản nhận 10 máy, mỗi máy 2kg.
                var type = new StorageSectionType { TypeCode = "GR-DEMO", Description = "Demo receiving" };
                var category = new Category { CategoryName = "Goods Receipt Demo" };
                var supplier = new Supplier { SupplierCode = "GR-DEMO", SupplierName = "Demo supplier", ContactPerson = "Demo", Status = "Active" };
                context.AddRange(type, category, supplier);
                await context.SaveChangesAsync();
                foreach (var warehouse in new[] { warehouseA, warehouseB })
                {
                    var section = new StorageSection
                    {
                        WarehouseId = warehouse.Id, StorageSectionTypeId = type.Id,
                        SectionCode = warehouse.WarehouseCode + "-S", SectionName = "Receiving", MaxRackCount = 10
                    };
                    var rack = new StorageRack
                    {
                        Section = section, RackCode = warehouse.WarehouseCode + "-R", MaxBinCount = 10, MaxWeightKg = 1000
                    };
                    context.StorageBins.Add(new StorageBin
                    {
                        Rack = rack, BinCode = warehouse.WarehouseCode + "-BIN", BinName = "Receiving", MaxWeightKg = 1000
                    });
                }
                var product = new Product
                {
                    CategoryId = category.Id, ProductCode = "GR-DEMO", ProductName = "Demo device",
                    Brand = "Demo", Model = "Demo", Status = "Active"
                };
                var variant = new ProductVariant
                {
                    Product = product, SKU = "GR-DEMO-SKU", WeightKg = 2, CostPrice = 100, SellingPrice = 200
                };
                context.ProductVariants.Add(variant);
                await context.SaveChangesAsync();

                // 3. Hai Approved riêng: một để thử 8 rồi 2, một để thử Reject.
                // Các trạng thái âm tính tách riêng, không ảnh hưởng happy path.
                foreach (string status in new[] { "Approved", "Approved", "Pending", "Rejected", "Completed" })
                {
                    var inbound = new InboundRequest
                    {
                        WarehouseId = warehouseA.Id, SupplierId = supplier.Id,
                        RequestedByUserId = purchasing.Id, ApprovedByUserId = manager.Id,
                        SupplierCommitmentStatus = "Confirmed", Status = status, CreatedDate = DateTime.UtcNow
                    };
                    context.InboundRequestItems.Add(new InboundRequestItem
                    {
                        InboundRequest = inbound, ProductVariantId = variant.Id, ExpectedQty = 10
                    });
                }
                // Mã chính thức dùng thử duplicate, nằm ở kho B để không làm lệch tồn kho A.
                var binB = await context.StorageBins.SingleAsync(x => x.BinCode == "GR-DEMO-B-BIN");
                context.SerialNumbers.Add(new SerialNumber
                {
                    ProductVariantId = variant.Id, CurrentBinId = binB.Id,
                    SerialCode = "GR-DEMO-EXISTING", CurrentStatus = "IN_STOCK"
                });
                context.Stocks.Add(new Stock { ProductVariantId = variant.Id, BinId = binB.Id, OnHandQty = 1, ReservedQty = 0 });
                await context.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            // Chỉ log ID demo, không log mật khẩu/connection string.
            var actors = await context.Users.Where(x => x.UserName.StartsWith("gr-demo-"))
                .Select(x => new { x.Id, x.UserName }).ToListAsync();
            var inbounds = await context.InboundRequests.Where(x => x.Warehouse.WarehouseCode == "GR-DEMO-A")
                .Select(x => new { x.Id, x.Status }).ToListAsync();
            var binA = await context.StorageBins.SingleAsync(x => x.BinCode == "GR-DEMO-A-BIN");
            logger.LogInformation("Goods Receipt demo: Users={Users}; Inbounds={Inbounds}; BinId={BinId}; SKU=GR-DEMO-SKU",
                System.Text.Json.JsonSerializer.Serialize(actors), System.Text.Json.JsonSerializer.Serialize(inbounds), binA.Id);
        }

        private static async Task<Role> GetRoleAsync(EWMSDbContext context, string name)
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

        private static User CreateUser(string name, int roleId)
        {
            return new User
            {
                UserName = name, FullName = name, Email = name + "@example.test", RoleId = roleId,
                PasswordHash = "DEVELOPMENT-HEADER-ONLY-NOT-A-LOGIN-HASH", Status = "Active"
            };
        }

        private static Warehouse CreateWarehouse(string code, int managerId)
        {
            return new Warehouse
            {
                WarehouseCode = code, WarehouseName = code, Address = "Demo fixture",
                ManagerUserId = managerId, Area = 100, MaxStorageSection = 10, MaxWeightKg = 1000, Status = "Active"
            };
        }
    }
}
