using EWMS.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Api.Data
{
    public class EWMSDbContext : DbContext
    {
        public EWMSDbContext(
            DbContextOptions<EWMSDbContext> options)
            : base(options)
        {
        }

        // 1. Authentication & User
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserWarehouse> UserWarehouses => Set<UserWarehouse>();

        // 2. Product & Supplier
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

        // 3. Warehouse & Stock
        public DbSet<StorageSectionType> StorageSectionTypes => Set<StorageSectionType>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<StorageSection> StorageSections => Set<StorageSection>();
        public DbSet<StorageRack> StorageRacks => Set<StorageRack>();
        public DbSet<StorageBin> StorageBins => Set<StorageBin>();
        public DbSet<Stock> Stocks => Set<Stock>();

        // 4. Inbound Request
        public DbSet<InboundRequest> InboundRequests => Set<InboundRequest>();
        public DbSet<InboundRequestItem> InboundRequestItems => Set<InboundRequestItem>();

        // 5. Goods Receipt & Serial
        public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
        public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
        public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();

        // 6. Transfer, Inventory, Outbound & History
        public DbSet<TransferOrder> TransferOrders => Set<TransferOrder>();
        public DbSet<TransferOrderItem> TransferOrderItems => Set<TransferOrderItem>();
        public DbSet<TransferOrderSerial> TransferOrderSerials => Set<TransferOrderSerial>();
        public DbSet<InventorySheet> InventorySheets => Set<InventorySheet>();
        public DbSet<InventorySheetItem> InventorySheetItems => Set<InventorySheetItem>();
        public DbSet<InventorySheetTrack> InventorySheetTracks => Set<InventorySheetTrack>();
        public DbSet<InventorySheetSerial> InventorySheetSerials => Set<InventorySheetSerial>();
        public DbSet<OutboundRequest> OutboundRequests => Set<OutboundRequest>();
        public DbSet<OutboundRequestItem> OutboundRequestItems => Set<OutboundRequestItem>();
        public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
        public DbSet<DeliveryOrderItem> DeliveryOrderItems => Set<DeliveryOrderItem>();
        public DbSet<SerialMovement> SerialMovements => Set<SerialMovement>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PERMISSION
            modelBuilder.Entity<Permission>(e =>
            {
                e.ToTable("Permission");
                e.HasKey(x => x.Id);
                e.Property(x => x.PermissionName)
                    .HasColumnType("varchar(100)").IsRequired();
                e.HasIndex(x => x.PermissionName).IsUnique();
                e.Property(x => x.Description)
                    .HasColumnType("varchar(255)");
            });

            // ROLE
            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("Role");
                e.HasKey(x => x.Id);
                e.Property(x => x.RoleName)
                    .HasColumnType("varchar(50)").IsRequired();
                e.HasIndex(x => x.RoleName).IsUnique();
                e.Property(x => x.Description)
                    .HasColumnType("varchar(255)");
            });

            // ROLE PERMISSION
            modelBuilder.Entity<RolePermission>(e =>
            {
                e.ToTable("RolePermission");
                e.HasKey(x => new { x.RoleId, x.PermissionId });

                e.HasOne(x => x.Role).WithMany()
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Permission).WithMany()
                    .HasForeignKey(x => x.PermissionId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // USER
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("User");
                e.HasKey(x => x.Id);

                e.Property(x => x.UserName)
                    .HasColumnType("varchar(50)").IsRequired();
                e.HasIndex(x => x.UserName).IsUnique();

                e.Property(x => x.PasswordHash)
                    .HasColumnType("varchar(255)").IsRequired();
                e.Property(x => x.FullName)
                    .HasColumnType("varchar(100)").IsRequired();
                e.Property(x => x.Email)
                    .HasColumnType("varchar(100)").IsRequired();
                e.Property(x => x.PhoneNumber)
                    .HasColumnType("varchar(20)");
                e.Property(x => x.Status)
                    .HasColumnType("varchar(20)").IsRequired();
                e.HasIndex(x => x.PhoneNumber).IsUnique();

                e.HasOne(x => x.Role).WithMany()
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // USER WAREHOUSE
            modelBuilder.Entity<UserWarehouse>(e =>
            {
                e.ToTable("UserWarehouse");
                e.HasKey(x => new { x.UserId, x.WarehouseId });

                e.HasOne(x => x.User).WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Warehouse).WithMany()
                    .HasForeignKey(x => x.WarehouseId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // CATEGORY

            modelBuilder.Entity<Category>(e =>
            {
                e.ToTable("Category");
                e.HasKey(x => x.Id);

                e.Property(x => x.CategoryName)
                    .HasColumnType("varchar(100)")
                    .IsRequired();

                e.HasIndex(x => x.CategoryName).IsUnique();

                e.HasOne(x => x.Parent)
                    .WithMany()
                    .HasForeignKey(x => x.ParentId)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // SUPPLIER
            modelBuilder.Entity<Supplier>(e =>
            {
                e.ToTable("Supplier");
                e.HasKey(x => x.Id);

                e.Property(x => x.SupplierCode)
                    .HasColumnType("varchar(50)").IsRequired();
                e.HasIndex(x => x.SupplierCode).IsUnique();

                e.Property(x => x.SupplierName)
                    .HasColumnType("varchar(100)").IsRequired();
                e.Property(x => x.ContactPerson)
                    .HasColumnType("varchar(100)").IsRequired();
                e.Property(x => x.Email)
                    .HasColumnType("varchar(100)");
                e.Property(x => x.PhoneNumber)
                    .HasColumnType("varchar(20)");
                e.Property(x => x.Address)
                    .HasColumnType("varchar(255)");
                e.Property(x => x.Status)
                    .HasColumnType("varchar(20)").IsRequired();
            });

            // PRODUCT
            modelBuilder.Entity<Product>(e =>
            {
                e.ToTable("Product");
                e.HasKey(x => x.Id);

                e.Property(x => x.ProductCode)
                    .HasColumnType("varchar(50)").IsRequired();
                e.HasIndex(x => x.ProductCode).IsUnique();

                e.Property(x => x.ProductName)
                    .HasColumnType("varchar(100)").IsRequired();
                e.Property(x => x.Brand)
                    .HasColumnType("varchar(100)").IsRequired();
                e.Property(x => x.Model)
                    .HasColumnType("varchar(100)").IsRequired();
                e.Property(x => x.Status)
                    .HasColumnType("varchar(20)").IsRequired();

                e.HasOne(x => x.Category).WithMany()
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // PRODUCT SUPPLIER
            modelBuilder.Entity<ProductSupplier>(e =>
            {
                e.ToTable("ProductSupplier");
                e.HasKey(x => new { x.ProductId, x.SupplierId });

                e.Property(x => x.SupplierSKU)
                    .HasColumnType("varchar(100)");
                e.Property(x => x.UnitPrice)
                    .HasColumnType("decimal(18,2)");

                e.HasOne(x => x.Product).WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Supplier).WithMany()
                    .HasForeignKey(x => x.SupplierId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // PRODUCT VARIANT
            modelBuilder.Entity<ProductVariant>(e =>
            {
                e.ToTable("ProductVariant");
                e.HasKey(x => x.Id);

                e.Property(x => x.SKU)
                    .HasColumnType("varchar(100)").IsRequired();
                e.HasIndex(x => x.SKU).IsUnique();

                e.Property(x => x.VariantName)
                    .HasColumnType("varchar(100)");
                e.Property(x => x.Color)
                    .HasColumnType("varchar(50)");
                e.Property(x => x.Capacity)
                    .HasColumnType("varchar(50)");
                e.Property(x => x.WeightKg)
                    .HasColumnType("decimal(10,2)");
                e.Property(x => x.CostPrice)
                    .HasColumnType("decimal(18,2)");
                e.Property(x => x.SellingPrice)
                    .HasColumnType("decimal(18,2)");
                e.Property(x => x.Specs)
                    .HasColumnType("varchar(500)");

                e.HasOne(x => x.Product).WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // STORAGE SECTION TYPE
            modelBuilder.Entity<StorageSectionType>(e =>
            {
                e.ToTable("StorageSectionType");
                e.HasKey(x => x.Id);

                e.Property(x => x.TypeCode)
                    .HasColumnType("varchar(50)").IsRequired();
                e.HasIndex(x => x.TypeCode).IsUnique();
                e.Property(x => x.Description)
                    .HasColumnType("varchar(255)");
            });

            // WAREHOUSE
            modelBuilder.Entity<Warehouse>(e =>
            {
                e.ToTable("Warehouse");
                e.HasKey(x => x.Id);

                e.Property(x => x.WarehouseCode)
                    .HasColumnType("varchar(50)").IsRequired();
                e.HasIndex(x => x.WarehouseCode).IsUnique();

                e.Property(x => x.WarehouseName)
                    .HasColumnType("varchar(100)").IsRequired();
                e.Property(x => x.Address)
                    .HasColumnType("varchar(255)").IsRequired();
                e.Property(x => x.PhoneNumber)
                    .HasColumnType("varchar(20)");
                e.Property(x => x.Area)
                    .HasColumnType("decimal(12,2)");
                e.Property(x => x.MaxWeightKg)
                    .HasColumnType("decimal(12,2)");
                e.Property(x => x.Status)
                    .HasColumnType("varchar(20)").IsRequired();

                e.HasOne(x => x.ManagerUser).WithMany()
                    .HasForeignKey(x => x.ManagerUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // STORAGE SECTION
            modelBuilder.Entity<StorageSection>(e =>
            {
                e.ToTable("StorageSection");
                e.HasKey(x => x.Id);

                e.Property(x => x.SectionCode)
                    .HasColumnType("varchar(50)").IsRequired();
                e.Property(x => x.SectionName)
                    .HasColumnType("varchar(100)").IsRequired();

                e.HasOne(x => x.Warehouse).WithMany()
                    .HasForeignKey(x => x.WarehouseId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.StorageSectionType).WithMany()
                    .HasForeignKey(x => x.StorageSectionTypeId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // STORAGE RACK
            modelBuilder.Entity<StorageRack>(e =>
            {
                e.ToTable("StorageRack");
                e.HasKey(x => x.Id);

                e.Property(x => x.RackCode)
                    .HasColumnType("varchar(50)").IsRequired();
                e.Property(x => x.MaxWeightKg)
                    .HasColumnType("decimal(10,2)");
                e.Property(x => x.LengthCm)
                    .HasColumnType("decimal(10,2)");
                e.Property(x => x.WidthCm)
                    .HasColumnType("decimal(10,2)");
                e.Property(x => x.HeightCm)
                    .HasColumnType("decimal(10,2)");

                e.HasOne(x => x.Section).WithMany()
                    .HasForeignKey(x => x.SectionId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // STORAGE BIN
            modelBuilder.Entity<StorageBin>(e =>
            {
                e.ToTable("StorageBin");
                e.HasKey(x => x.Id);

                e.Property(x => x.BinCode)
                    .HasColumnType("varchar(50)").IsRequired();
                e.Property(x => x.BinName)
                    .HasColumnType("varchar(100)").IsRequired();
                e.Property(x => x.MaxWeightKg)
                    .HasColumnType("decimal(10,2)");

                e.HasOne(x => x.Rack).WithMany()
                    .HasForeignKey(x => x.RackId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // STOCK
            modelBuilder.Entity<Stock>(e =>
            {
                e.ToTable("Stock");
                e.HasKey(x => x.Id);

                e.HasIndex(x => new {
                    x.ProductVariantId,
                    x.BinId
                }).IsUnique();

                e.HasOne(x => x.ProductVariant).WithMany()
                    .HasForeignKey(x => x.ProductVariantId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Bin).WithMany()
                    .HasForeignKey(x => x.BinId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // INBOUND REQUEST
            modelBuilder.Entity<InboundRequest>(e =>
            {
                e.ToTable("InboundRequest");
                e.HasKey(x => x.Id);

                e.Property(x => x.SupplierCommitmentStatus)
                    .HasColumnType("varchar(30)");
                e.Property(x => x.ExpectedDeliveryDate)
                    .HasColumnType("datetime2");
                e.Property(x => x.RejectionReason)
                    .HasColumnType("varchar(255)");
                e.Property(x => x.Status)
                    .HasColumnType("varchar(30)").IsRequired();
                e.Property(x => x.CreatedDate)
                    .HasColumnType("datetime2");

                e.HasOne(x => x.Warehouse).WithMany()
                    .HasForeignKey(x => x.WarehouseId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Supplier).WithMany()
                    .HasForeignKey(x => x.SupplierId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.RequestedByUser).WithMany()
                    .HasForeignKey(x => x.RequestedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ApprovedByUser).WithMany()
                    .HasForeignKey(x => x.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // INBOUND REQUEST ITEM
            modelBuilder.Entity<InboundRequestItem>(e =>
            {
                e.ToTable("InboundRequestItem");
                e.HasKey(x => x.Id);

                e.HasOne(x => x.InboundRequest).WithMany()
                    .HasForeignKey(x => x.InboundRequestId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ProductVariant).WithMany()
                    .HasForeignKey(x => x.ProductVariantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // GOODS RECEIPT
            modelBuilder.Entity<GoodsReceipt>(e =>
            {
                e.ToTable("GoodsReceipt");
                e.HasKey(x => x.Id);

                e.Property(x => x.ReceivedDate)
                    .HasColumnType("datetime2");
                e.Property(x => x.Status)
                    .HasColumnType("varchar(30)").IsRequired();
                e.Property(x => x.ApprovalDate)
                    .HasColumnType("datetime2");
                e.Property(x => x.RejectionReason)
                    .HasColumnType("varchar(255)");

                e.HasOne(x => x.InboundRequest).WithMany()
                    .HasForeignKey(x => x.InboundRequestId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ReceivedByUser).WithMany()
                    .HasForeignKey(x => x.ReceivedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ApprovedByUser).WithMany()
                    .HasForeignKey(x => x.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // GOODS RECEIPT ITEM
            modelBuilder.Entity<GoodsReceiptItem>(e =>
            {
                e.ToTable("GoodsReceiptItem");
                e.HasKey(x => x.Id);

                e.HasOne(x => x.GoodsReceipt).WithMany()
                    .HasForeignKey(x => x.GoodsReceiptId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ProductVariant).WithMany()
                    .HasForeignKey(x => x.ProductVariantId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Bin).WithMany()
                    .HasForeignKey(x => x.BinId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // SERIAL NUMBER
            modelBuilder.Entity<SerialNumber>(e =>
            {
                e.ToTable("SerialNumber");
                e.HasKey(x => x.Id);

                e.Property(x => x.SerialCode)
                    .HasColumnType("varchar(100)").IsRequired();
                e.HasIndex(x => x.SerialCode).IsUnique();

                e.Property(x => x.CurrentStatus)
                    .HasColumnType("varchar(30)").IsRequired();

                e.HasOne(x => x.ProductVariant).WithMany()
                    .HasForeignKey(x => x.ProductVariantId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.OriginGoodsReceiptItem).WithMany()
                    .HasForeignKey(x => x.OriginGoodsReceiptItemId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.CurrentBin).WithMany()
                    .HasForeignKey(x => x.CurrentBinId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ReservedForOutboundItem)
                    .WithMany(x => x.ReservedSerialNumbers)
                    .HasForeignKey(x => x.ReservedForOutboundItemId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.DispatchedInDeliveryItem)
                    .WithMany(x => x.DispatchedSerialNumbers)
                    .HasForeignKey(x => x.DispatchedInDeliveryItemId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // TransferOrder
            modelBuilder.Entity<TransferOrder>(e =>
            {
                e.ToTable("TransferOrder");
                e.HasKey(x => x.Id);

                e.Property(x => x.Status)
                    .HasColumnType("varchar(30)").IsRequired();

                e.Property(x => x.CreatedDate)
                    .HasColumnType("datetime2").IsRequired();

                e.HasOne(x => x.FromWarehouse).WithMany()
                    .HasForeignKey(x => x.FromWarehouseId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ToWarehouse).WithMany()
                    .HasForeignKey(x => x.ToWarehouseId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.RequestedByUser).WithMany()
                    .HasForeignKey(x => x.RequestedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ApprovedByUser).WithMany()
                    .HasForeignKey(x => x.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.DestinationStaffUser).WithMany()
                    .HasForeignKey(x => x.DestinationStaffUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // TransferOrderItem
            modelBuilder.Entity<TransferOrderItem>(e =>
            {
                e.ToTable("TransferOrderItem");
                e.HasKey(x => x.Id);

                e.Property(x => x.DiscrepancyNote)
                    .HasColumnType("varchar(255)");

                e.HasOne(x => x.TransferOrder).WithMany(x => x.Items)
                    .HasForeignKey(x => x.TransferOrderId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ProductVariant).WithMany()
                    .HasForeignKey(x => x.ProductVariantId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.FromBin).WithMany()
                    .HasForeignKey(x => x.FromBinId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ToBin).WithMany()
                    .HasForeignKey(x => x.ToBinId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // TransferOrderSerial
            modelBuilder.Entity<TransferOrderSerial>(e =>
            {
                e.ToTable("TransferOrderSerial");
                e.HasKey(x => x.Id);

                e.Property(x => x.Status)
                    .HasColumnType("varchar(30)").IsRequired();

                e.Property(x => x.DispatchedAt)
                    .HasColumnType("datetime2");

                e.Property(x => x.ReceivedAt)
                    .HasColumnType("datetime2");

                e.HasOne(x => x.TransferOrderItem).WithMany(x => x.Serials)
                    .HasForeignKey(x => x.TransferOrderItemId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.SerialNumber).WithMany()
                    .HasForeignKey(x => x.SerialNumberId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // InventorySheet
            modelBuilder.Entity<InventorySheet>(e =>
            {
                e.ToTable("InventorySheet");
                e.HasKey(x => x.Id);

                e.Property(x => x.Status)
                    .HasColumnType("varchar(30)").IsRequired();

                e.Property(x => x.RejectionReason)
                    .HasColumnType("varchar(255)");

                e.Property(x => x.CreatedDate)
                    .HasColumnType("datetime2").IsRequired();

                e.HasOne(x => x.Warehouse).WithMany()
                    .HasForeignKey(x => x.WarehouseId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.CreatedByUser).WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ApprovedByUser).WithMany()
                    .HasForeignKey(x => x.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // InventorySheetItem
            modelBuilder.Entity<InventorySheetItem>(e =>
            {
                e.ToTable("InventorySheetItem");
                e.HasKey(x => x.Id);

                e.Property(x => x.Note)
                    .HasColumnType("varchar(255)");

                e.HasOne(x => x.InventorySheet).WithMany(x => x.Items)
                    .HasForeignKey(x => x.InventorySheetId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Bin).WithMany()
                    .HasForeignKey(x => x.BinId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ProductVariant).WithMany()
                    .HasForeignKey(x => x.ProductVariantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // InventorySheetTrack
            modelBuilder.Entity<InventorySheetTrack>(e =>
            {
                e.ToTable("InventorySheetTrack");
                e.HasKey(x => x.Id);

                e.Property(x => x.Note)
                    .HasColumnType("varchar(255)");

                e.Property(x => x.CountedAt)
                    .HasColumnType("datetime2").IsRequired();

                e.HasOne(x => x.InventorySheetItem).WithMany(x => x.Tracks)
                    .HasForeignKey(x => x.InventorySheetItemId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.CountedByUser).WithMany()
                    .HasForeignKey(x => x.CountedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // InventorySheetSerial
            modelBuilder.Entity<InventorySheetSerial>(e =>
            {
                e.ToTable("InventorySheetSerial");
                e.HasKey(x => x.Id);

                e.Property(x => x.ResultStatus)
                    .HasColumnType("varchar(30)").IsRequired();

                e.Property(x => x.ScannedAt)
                    .HasColumnType("datetime2");

                e.HasOne(x => x.InventorySheet).WithMany(x => x.Serials)
                    .HasForeignKey(x => x.InventorySheetId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.SerialNumber).WithMany()
                    .HasForeignKey(x => x.SerialNumberId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ExpectedBin).WithMany()
                    .HasForeignKey(x => x.ExpectedBinId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ScannedBin).WithMany()
                    .HasForeignKey(x => x.ScannedBinId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.CountedByUser).WithMany()
                    .HasForeignKey(x => x.CountedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // OutboundRequest
            modelBuilder.Entity<OutboundRequest>(e =>
            {
                e.ToTable("OutboundRequest");
                e.HasKey(x => x.Id);

                e.Property(x => x.CustomerName)
                    .HasColumnType("varchar(100)");

                e.Property(x => x.CustomerEmail)
                    .HasColumnType("varchar(100)");

                e.Property(x => x.CustomerPhone)
                    .HasColumnType("varchar(20)");

                e.Property(x => x.DestinationAddress)
                    .HasColumnType("varchar(255)");

                e.Property(x => x.Reason)
                    .HasColumnType("varchar(255)");

                e.Property(x => x.RejectionReason)
                    .HasColumnType("varchar(255)");

                e.Property(x => x.Status)
                    .HasColumnType("varchar(30)").IsRequired();

                e.Property(x => x.CreatedDate)
                    .HasColumnType("datetime2").IsRequired();

                e.HasOne(x => x.Warehouse).WithMany()
                    .HasForeignKey(x => x.WarehouseId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.RequestedByUser).WithMany()
                    .HasForeignKey(x => x.RequestedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ApprovedByUser).WithMany()
                    .HasForeignKey(x => x.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // OutboundRequestItem
            modelBuilder.Entity<OutboundRequestItem>(e =>
            {
                e.ToTable("OutboundRequestItem");
                e.HasKey(x => x.Id);

                e.HasOne(x => x.OutboundRequest).WithMany(x => x.Items)
                    .HasForeignKey(x => x.OutboundRequestId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ProductVariant).WithMany()
                    .HasForeignKey(x => x.ProductVariantId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // DeliveryOrder
            modelBuilder.Entity<DeliveryOrder>(e =>
            {
                e.ToTable("DeliveryOrder");
                e.HasKey(x => x.Id);

                e.Property(x => x.DeliveryDate)
                    .HasColumnType("datetime2");

                e.Property(x => x.Status)
                    .HasColumnType("varchar(30)").IsRequired();

                e.HasOne(x => x.OutboundRequest).WithMany(x => x.DeliveryOrders)
                    .HasForeignKey(x => x.OutboundRequestId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.CreatedByUser).WithMany()
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // DeliveryOrderItem
            modelBuilder.Entity<DeliveryOrderItem>(e =>
            {
                e.ToTable("DeliveryOrderItem");
                e.HasKey(x => x.Id);

                e.HasOne(x => x.DeliveryOrder).WithMany(x => x.Items)
                    .HasForeignKey(x => x.DeliveryOrderId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ProductVariant).WithMany()
                    .HasForeignKey(x => x.ProductVariantId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Bin).WithMany()
                    .HasForeignKey(x => x.BinId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // SerialMovement
            modelBuilder.Entity<SerialMovement>(e =>
            {
                e.ToTable("SerialMovement");
                e.HasKey(x => x.Id);

                e.Property(x => x.MovementType)
                    .HasColumnType("varchar(50)").IsRequired();

                e.Property(x => x.RefType)
                    .HasColumnType("varchar(50)");

                e.Property(x => x.MovementDate)
                    .HasColumnType("datetime2").IsRequired();

                e.HasOne(x => x.SerialNumber).WithMany()
                    .HasForeignKey(x => x.SerialNumberId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.FromBin).WithMany()
                    .HasForeignKey(x => x.FromBinId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.ToBin).WithMany()
                    .HasForeignKey(x => x.ToBinId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.PerformedByUser).WithMany()
                    .HasForeignKey(x => x.PerformedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // AuditLog
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.ToTable("AuditLog");
                e.HasKey(x => x.Id);

                e.Property(x => x.Action)
                    .HasColumnType("varchar(100)").IsRequired();

                e.Property(x => x.EntityName)
                    .HasColumnType("varchar(100)").IsRequired();

                e.Property(x => x.OldValue)
                    .HasColumnType("text");

                e.Property(x => x.NewValue)
                    .HasColumnType("text");

                e.Property(x => x.CreatedAt)
                    .HasColumnType("datetime2").IsRequired();

                e.HasOne(x => x.User).WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
