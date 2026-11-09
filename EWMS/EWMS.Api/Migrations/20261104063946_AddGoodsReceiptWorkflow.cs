using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BinId",
                table: "GoodsReceiptItem",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "GoodsReceipt",
                type: "nvarchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "GoodsReceipt",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "GoodsReceipt",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_GoodsReceiptItem_Id_GoodsReceiptId",
                table: "GoodsReceiptItem",
                columns: new[] { "Id", "GoodsReceiptId" });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptSerial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsReceiptId = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptItemId = table.Column<int>(type: "int", nullable: false),
                    SerialCode = table.Column<string>(type: "varchar(100)", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScannedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptSerial_GoodsReceiptItem_GoodsReceiptItemId_GoodsReceiptId",
                        columns: x => new { x.GoodsReceiptItemId, x.GoodsReceiptId },
                        principalTable: "GoodsReceiptItem",
                        principalColumns: new[] { "Id", "GoodsReceiptId" });
                    table.ForeignKey(
                        name: "FK_GoodsReceiptSerial_User_ScannedByUserId",
                        column: x => x.ScannedByUserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptSerial_GoodsReceiptId_SerialCode",
                table: "GoodsReceiptSerial",
                columns: new[] { "GoodsReceiptId", "SerialCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptSerial_GoodsReceiptItemId_GoodsReceiptId",
                table: "GoodsReceiptSerial",
                columns: new[] { "GoodsReceiptItemId", "GoodsReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptSerial_ScannedByUserId",
                table: "GoodsReceiptSerial",
                column: "ScannedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không được biến bin nháp thành BinId=0 khi rollback schema.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [GoodsReceiptItem] WHERE [BinId] IS NULL) THROW 51002, 'Unplaced draft items must be resolved before schema rollback.', 1;");

            migrationBuilder.DropTable(
                name: "GoodsReceiptSerial");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_GoodsReceiptItem_Id_GoodsReceiptId",
                table: "GoodsReceiptItem");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GoodsReceipt");

            migrationBuilder.AlterColumn<int>(
                name: "BinId",
                table: "GoodsReceiptItem",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "GoodsReceipt",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldNullable: true);
        }
    }
}
