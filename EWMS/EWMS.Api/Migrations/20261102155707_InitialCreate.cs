using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    CategoryName = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Category_Category_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Category",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionName = table.Column<string>(type: "varchar(100)", nullable: false),
                    Description = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "varchar(50)", nullable: false),
                    Description = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageSectionType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    Description = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageSectionType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Supplier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    SupplierName = table.Column<string>(type: "varchar(100)", nullable: false),
                    ContactPerson = table.Column<string>(type: "varchar(100)", nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", nullable: true),
                    Address = table.Column<string>(type: "varchar(255)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ProductCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    ProductName = table.Column<string>(type: "varchar(100)", nullable: false),
                    Brand = table.Column<string>(type: "varchar(100)", nullable: false),
                    Model = table.Column<string>(type: "varchar(100)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermission_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "varchar(50)", nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", nullable: false),
                    FullName = table.Column<string>(type: "varchar(100)", nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductSupplier",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    SupplierSKU = table.Column<string>(type: "varchar(100)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    IsPreferred = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSupplier", x => new { x.ProductId, x.SupplierId });
                    table.ForeignKey(
                        name: "FK_ProductSupplier_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductSupplier_Supplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Supplier",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductVariant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    SKU = table.Column<string>(type: "varchar(100)", nullable: false),
                    VariantName = table.Column<string>(type: "varchar(100)", nullable: true),
                    Color = table.Column<string>(type: "varchar(50)", nullable: true),
                    Capacity = table.Column<string>(type: "varchar(50)", nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CostPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WarrantyMonths = table.Column<int>(type: "int", nullable: true),
                    Specs = table.Column<string>(type: "varchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariant_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Warehouse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    WarehouseName = table.Column<string>(type: "varchar(100)", nullable: false),
                    Address = table.Column<string>(type: "varchar(255)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", nullable: true),
                    ManagerUserId = table.Column<int>(type: "int", nullable: false),
                    Area = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    MaxStorageSection = table.Column<int>(type: "int", nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warehouse_User_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InboundRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    SupplierCommitmentStatus = table.Column<string>(type: "varchar(30)", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "varchar(255)", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundRequest_Supplier_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Supplier",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InboundRequest_User_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InboundRequest_User_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InboundRequest_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StorageSection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    StorageSectionTypeId = table.Column<int>(type: "int", nullable: false),
                    SectionCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    SectionName = table.Column<string>(type: "varchar(100)", nullable: false),
                    MaxRackCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageSection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageSection_StorageSectionType_StorageSectionTypeId",
                        column: x => x.StorageSectionTypeId,
                        principalTable: "StorageSectionType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StorageSection_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserWarehouse",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWarehouse", x => new { x.UserId, x.WarehouseId });
                    table.ForeignKey(
                        name: "FK_UserWarehouse_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserWarehouse_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InboundRequestId = table.Column<int>(type: "int", nullable: false),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_InboundRequest_InboundRequestId",
                        column: x => x.InboundRequestId,
                        principalTable: "InboundRequest",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_User_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_User_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InboundRequestItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InboundRequestId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    ExpectedQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundRequestItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundRequestItem_InboundRequest_InboundRequestId",
                        column: x => x.InboundRequestId,
                        principalTable: "InboundRequest",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InboundRequestItem_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StorageRack",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    RackCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    MaxBinCount = table.Column<int>(type: "int", nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LengthCm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    WidthCm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageRack", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageRack_StorageSection_SectionId",
                        column: x => x.SectionId,
                        principalTable: "StorageSection",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StorageBin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RackId = table.Column<int>(type: "int", nullable: false),
                    BinCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    BinName = table.Column<string>(type: "varchar(100)", nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageBin", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageBin_StorageRack_RackId",
                        column: x => x.RackId,
                        principalTable: "StorageRack",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsReceiptId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    BinId = table.Column<int>(type: "int", nullable: false),
                    ReceivedQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItem_GoodsReceipt_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipt",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItem_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItem_StorageBin_BinId",
                        column: x => x.BinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Stock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    BinId = table.Column<int>(type: "int", nullable: false),
                    OnHandQty = table.Column<int>(type: "int", nullable: false),
                    ReservedQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stock_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Stock_StorageBin_BinId",
                        column: x => x.BinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SerialNumber",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    OriginGoodsReceiptItemId = table.Column<int>(type: "int", nullable: true),
                    ReservedForOutboundItemId = table.Column<int>(type: "int", nullable: true),
                    CurrentBinId = table.Column<int>(type: "int", nullable: true),
                    DispatchedInDeliveryItemId = table.Column<int>(type: "int", nullable: true),
                    SerialCode = table.Column<string>(type: "varchar(100)", nullable: false),
                    CurrentStatus = table.Column<string>(type: "varchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialNumber", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SerialNumber_GoodsReceiptItem_OriginGoodsReceiptItemId",
                        column: x => x.OriginGoodsReceiptItemId,
                        principalTable: "GoodsReceiptItem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SerialNumber_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SerialNumber_StorageBin_CurrentBinId",
                        column: x => x.CurrentBinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Category_CategoryName",
                table: "Category",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Category_ParentId",
                table: "Category",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_ApprovedByUserId",
                table: "GoodsReceipt",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_InboundRequestId",
                table: "GoodsReceipt",
                column: "InboundRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_ReceivedByUserId",
                table: "GoodsReceipt",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItem_BinId",
                table: "GoodsReceiptItem",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItem_GoodsReceiptId",
                table: "GoodsReceiptItem",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItem_ProductVariantId",
                table: "GoodsReceiptItem",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundRequest_ApprovedByUserId",
                table: "InboundRequest",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundRequest_RequestedByUserId",
                table: "InboundRequest",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundRequest_SupplierId",
                table: "InboundRequest",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundRequest_WarehouseId",
                table: "InboundRequest",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundRequestItem_InboundRequestId",
                table: "InboundRequestItem",
                column: "InboundRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundRequestItem_ProductVariantId",
                table: "InboundRequestItem",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_PermissionName",
                table: "Permission",
                column: "PermissionName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_CategoryId",
                table: "Product",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_ProductCode",
                table: "Product",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSupplier_SupplierId",
                table: "ProductSupplier",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_ProductId",
                table: "ProductVariant",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_SKU",
                table: "ProductVariant",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_RoleName",
                table: "Role",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumber_CurrentBinId",
                table: "SerialNumber",
                column: "CurrentBinId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumber_OriginGoodsReceiptItemId",
                table: "SerialNumber",
                column: "OriginGoodsReceiptItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumber_ProductVariantId",
                table: "SerialNumber",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumber_SerialCode",
                table: "SerialNumber",
                column: "SerialCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stock_BinId",
                table: "Stock",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_ProductVariantId_BinId",
                table: "Stock",
                columns: new[] { "ProductVariantId", "BinId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorageBin_RackId",
                table: "StorageBin",
                column: "RackId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageRack_SectionId",
                table: "StorageRack",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageSection_StorageSectionTypeId",
                table: "StorageSection",
                column: "StorageSectionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageSection_WarehouseId",
                table: "StorageSection",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageSectionType_TypeCode",
                table: "StorageSectionType",
                column: "TypeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_SupplierCode",
                table: "Supplier",
                column: "SupplierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleId",
                table: "User",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_User_UserName",
                table: "User",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWarehouse_WarehouseId",
                table: "UserWarehouse",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_ManagerUserId",
                table: "Warehouse",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_WarehouseCode",
                table: "Warehouse",
                column: "WarehouseCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboundRequestItem");

            migrationBuilder.DropTable(
                name: "ProductSupplier");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "SerialNumber");

            migrationBuilder.DropTable(
                name: "Stock");

            migrationBuilder.DropTable(
                name: "UserWarehouse");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "GoodsReceiptItem");

            migrationBuilder.DropTable(
                name: "GoodsReceipt");

            migrationBuilder.DropTable(
                name: "ProductVariant");

            migrationBuilder.DropTable(
                name: "StorageBin");

            migrationBuilder.DropTable(
                name: "InboundRequest");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "StorageRack");

            migrationBuilder.DropTable(
                name: "Supplier");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "StorageSection");

            migrationBuilder.DropTable(
                name: "StorageSectionType");

            migrationBuilder.DropTable(
                name: "Warehouse");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
