using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class CompleteSdsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(100)", nullable: false),
                    EntityName = table.Column<string>(type: "varchar(100)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLog_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventorySheet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false),
                    RejectionReason = table.Column<string>(type: "varchar(255)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySheet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventorySheet_User_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventorySheet_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventorySheet_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OutboundRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "varchar(100)", nullable: true),
                    CustomerEmail = table.Column<string>(type: "varchar(100)", nullable: true),
                    CustomerPhone = table.Column<string>(type: "varchar(20)", nullable: true),
                    DestinationAddress = table.Column<string>(type: "varchar(255)", nullable: true),
                    Reason = table.Column<string>(type: "varchar(255)", nullable: true),
                    RejectionReason = table.Column<string>(type: "varchar(255)", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundRequest_User_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutboundRequest_User_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutboundRequest_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SerialMovement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SerialNumberId = table.Column<int>(type: "int", nullable: false),
                    FromBinId = table.Column<int>(type: "int", nullable: true),
                    ToBinId = table.Column<int>(type: "int", nullable: true),
                    PerformedByUserId = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<string>(type: "varchar(50)", nullable: false),
                    RefType = table.Column<string>(type: "varchar(50)", nullable: true),
                    RefId = table.Column<int>(type: "int", nullable: true),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SerialMovement_SerialNumber_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumber",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SerialMovement_StorageBin_FromBinId",
                        column: x => x.FromBinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SerialMovement_StorageBin_ToBinId",
                        column: x => x.ToBinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SerialMovement_User_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransferOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromWarehouseId = table.Column<int>(type: "int", nullable: false),
                    ToWarehouseId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    DestinationStaffUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferOrder_User_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrder_User_DestinationStaffUserId",
                        column: x => x.DestinationStaffUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrder_User_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrder_Warehouse_FromWarehouseId",
                        column: x => x.FromWarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrder_Warehouse_ToWarehouseId",
                        column: x => x.ToWarehouseId,
                        principalTable: "Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventorySheetItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventorySheetId = table.Column<int>(type: "int", nullable: false),
                    BinId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    SystemQty = table.Column<int>(type: "int", nullable: false),
                    ActualQty = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySheetItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventorySheetItem_InventorySheet_InventorySheetId",
                        column: x => x.InventorySheetId,
                        principalTable: "InventorySheet",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventorySheetItem_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventorySheetItem_StorageBin_BinId",
                        column: x => x.BinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventorySheetSerial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventorySheetId = table.Column<int>(type: "int", nullable: false),
                    SerialNumberId = table.Column<int>(type: "int", nullable: false),
                    ExpectedBinId = table.Column<int>(type: "int", nullable: true),
                    ScannedBinId = table.Column<int>(type: "int", nullable: true),
                    IsExpected = table.Column<bool>(type: "bit", nullable: false),
                    IsScanned = table.Column<bool>(type: "bit", nullable: false),
                    ResultStatus = table.Column<string>(type: "varchar(30)", nullable: false),
                    CountedByUserId = table.Column<int>(type: "int", nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySheetSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventorySheetSerial_InventorySheet_InventorySheetId",
                        column: x => x.InventorySheetId,
                        principalTable: "InventorySheet",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventorySheetSerial_SerialNumber_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumber",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventorySheetSerial_StorageBin_ExpectedBinId",
                        column: x => x.ExpectedBinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventorySheetSerial_StorageBin_ScannedBinId",
                        column: x => x.ScannedBinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventorySheetSerial_User_CountedByUserId",
                        column: x => x.CountedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboundRequestId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryOrder_OutboundRequest_OutboundRequestId",
                        column: x => x.OutboundRequestId,
                        principalTable: "OutboundRequest",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryOrder_User_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OutboundRequestItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboundRequestId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    RequestedQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundRequestItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundRequestItem_OutboundRequest_OutboundRequestId",
                        column: x => x.OutboundRequestId,
                        principalTable: "OutboundRequest",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutboundRequestItem_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransferOrderItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    FromBinId = table.Column<int>(type: "int", nullable: false),
                    ToBinId = table.Column<int>(type: "int", nullable: false),
                    RequestedQty = table.Column<int>(type: "int", nullable: false),
                    ActualReceivedQty = table.Column<int>(type: "int", nullable: true),
                    DiscrepancyNote = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferOrderItem_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrderItem_StorageBin_FromBinId",
                        column: x => x.FromBinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrderItem_StorageBin_ToBinId",
                        column: x => x.ToBinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrderItem_TransferOrder_TransferOrderId",
                        column: x => x.TransferOrderId,
                        principalTable: "TransferOrder",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventorySheetTrack",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventorySheetItemId = table.Column<int>(type: "int", nullable: false),
                    CountedByUserId = table.Column<int>(type: "int", nullable: false),
                    CountedQty = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "varchar(255)", nullable: true),
                    CountedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySheetTrack", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventorySheetTrack_InventorySheetItem_InventorySheetItemId",
                        column: x => x.InventorySheetItemId,
                        principalTable: "InventorySheetItem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventorySheetTrack_User_CountedByUserId",
                        column: x => x.CountedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrderItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    BinId = table.Column<int>(type: "int", nullable: false),
                    DispatchedQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrderItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryOrderItem_DeliveryOrder_DeliveryOrderId",
                        column: x => x.DeliveryOrderId,
                        principalTable: "DeliveryOrder",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryOrderItem_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryOrderItem_StorageBin_BinId",
                        column: x => x.BinId,
                        principalTable: "StorageBin",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransferOrderSerial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferOrderItemId = table.Column<int>(type: "int", nullable: false),
                    SerialNumberId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false),
                    DispatchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferOrderSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferOrderSerial_SerialNumber_SerialNumberId",
                        column: x => x.SerialNumberId,
                        principalTable: "SerialNumber",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrderSerial_TransferOrderItem_TransferOrderItemId",
                        column: x => x.TransferOrderItemId,
                        principalTable: "TransferOrderItem",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_PhoneNumber",
                table: "User",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumber_DispatchedInDeliveryItemId",
                table: "SerialNumber",
                column: "DispatchedInDeliveryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialNumber_ReservedForOutboundItemId",
                table: "SerialNumber",
                column: "ReservedForOutboundItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_CreatedByUserId",
                table: "DeliveryOrder",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_OutboundRequestId",
                table: "DeliveryOrder",
                column: "OutboundRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderItem_BinId",
                table: "DeliveryOrderItem",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderItem_DeliveryOrderId",
                table: "DeliveryOrderItem",
                column: "DeliveryOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderItem_ProductVariantId",
                table: "DeliveryOrderItem",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheet_ApprovedByUserId",
                table: "InventorySheet",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheet_CreatedByUserId",
                table: "InventorySheet",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheet_WarehouseId",
                table: "InventorySheet",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetItem_BinId",
                table: "InventorySheetItem",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetItem_InventorySheetId",
                table: "InventorySheetItem",
                column: "InventorySheetId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetItem_ProductVariantId",
                table: "InventorySheetItem",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetSerial_CountedByUserId",
                table: "InventorySheetSerial",
                column: "CountedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetSerial_ExpectedBinId",
                table: "InventorySheetSerial",
                column: "ExpectedBinId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetSerial_InventorySheetId",
                table: "InventorySheetSerial",
                column: "InventorySheetId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetSerial_ScannedBinId",
                table: "InventorySheetSerial",
                column: "ScannedBinId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetSerial_SerialNumberId",
                table: "InventorySheetSerial",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetTrack_CountedByUserId",
                table: "InventorySheetTrack",
                column: "CountedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySheetTrack_InventorySheetItemId",
                table: "InventorySheetTrack",
                column: "InventorySheetItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundRequest_ApprovedByUserId",
                table: "OutboundRequest",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundRequest_RequestedByUserId",
                table: "OutboundRequest",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundRequest_WarehouseId",
                table: "OutboundRequest",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundRequestItem_OutboundRequestId",
                table: "OutboundRequestItem",
                column: "OutboundRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundRequestItem_ProductVariantId",
                table: "OutboundRequestItem",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialMovement_FromBinId",
                table: "SerialMovement",
                column: "FromBinId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialMovement_PerformedByUserId",
                table: "SerialMovement",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialMovement_SerialNumberId",
                table: "SerialMovement",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_SerialMovement_ToBinId",
                table: "SerialMovement",
                column: "ToBinId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrder_ApprovedByUserId",
                table: "TransferOrder",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrder_DestinationStaffUserId",
                table: "TransferOrder",
                column: "DestinationStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrder_FromWarehouseId",
                table: "TransferOrder",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrder_RequestedByUserId",
                table: "TransferOrder",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrder_ToWarehouseId",
                table: "TransferOrder",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderItem_FromBinId",
                table: "TransferOrderItem",
                column: "FromBinId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderItem_ProductVariantId",
                table: "TransferOrderItem",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderItem_ToBinId",
                table: "TransferOrderItem",
                column: "ToBinId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderItem_TransferOrderId",
                table: "TransferOrderItem",
                column: "TransferOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderSerial_SerialNumberId",
                table: "TransferOrderSerial",
                column: "SerialNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderSerial_TransferOrderItemId",
                table: "TransferOrderSerial",
                column: "TransferOrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SerialNumber_DeliveryOrderItem_DispatchedInDeliveryItemId",
                table: "SerialNumber",
                column: "DispatchedInDeliveryItemId",
                principalTable: "DeliveryOrderItem",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SerialNumber_OutboundRequestItem_ReservedForOutboundItemId",
                table: "SerialNumber",
                column: "ReservedForOutboundItemId",
                principalTable: "OutboundRequestItem",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SerialNumber_DeliveryOrderItem_DispatchedInDeliveryItemId",
                table: "SerialNumber");

            migrationBuilder.DropForeignKey(
                name: "FK_SerialNumber_OutboundRequestItem_ReservedForOutboundItemId",
                table: "SerialNumber");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "DeliveryOrderItem");

            migrationBuilder.DropTable(
                name: "InventorySheetSerial");

            migrationBuilder.DropTable(
                name: "InventorySheetTrack");

            migrationBuilder.DropTable(
                name: "OutboundRequestItem");

            migrationBuilder.DropTable(
                name: "SerialMovement");

            migrationBuilder.DropTable(
                name: "TransferOrderSerial");

            migrationBuilder.DropTable(
                name: "DeliveryOrder");

            migrationBuilder.DropTable(
                name: "InventorySheetItem");

            migrationBuilder.DropTable(
                name: "TransferOrderItem");

            migrationBuilder.DropTable(
                name: "OutboundRequest");

            migrationBuilder.DropTable(
                name: "InventorySheet");

            migrationBuilder.DropTable(
                name: "TransferOrder");

            migrationBuilder.DropIndex(
                name: "IX_User_PhoneNumber",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumber_DispatchedInDeliveryItemId",
                table: "SerialNumber");

            migrationBuilder.DropIndex(
                name: "IX_SerialNumber_ReservedForOutboundItemId",
                table: "SerialNumber");
        }
    }
}
