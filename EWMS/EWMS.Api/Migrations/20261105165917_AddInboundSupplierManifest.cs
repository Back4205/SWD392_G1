using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInboundSupplierManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasSupplierManifest",
                table: "InboundRequest",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_InboundRequestItem_Id_InboundRequestId",
                table: "InboundRequestItem",
                columns: new[] { "Id", "InboundRequestId" });

            migrationBuilder.CreateTable(
                name: "InboundExpectedSerial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InboundRequestId = table.Column<int>(type: "int", nullable: false),
                    InboundRequestItemId = table.Column<int>(type: "int", nullable: false),
                    SerialCode = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundExpectedSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundExpectedSerial_InboundRequestItem_InboundRequestItemId_InboundRequestId",
                        columns: x => new { x.InboundRequestItemId, x.InboundRequestId },
                        principalTable: "InboundRequestItem",
                        principalColumns: new[] { "Id", "InboundRequestId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboundExpectedSerial_InboundRequestId_SerialCode",
                table: "InboundExpectedSerial",
                columns: new[] { "InboundRequestId", "SerialCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboundExpectedSerial_InboundRequestItemId_InboundRequestId",
                table: "InboundExpectedSerial",
                columns: new[] { "InboundRequestItemId", "InboundRequestId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboundExpectedSerial");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_InboundRequestItem_Id_InboundRequestId",
                table: "InboundRequestItem");

            migrationBuilder.DropColumn(
                name: "HasSupplierManifest",
                table: "InboundRequest");
        }
    }
}
