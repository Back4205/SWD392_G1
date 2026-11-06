using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedInboundStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedStaffUserId",
                table: "InboundRequest",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboundRequest_AssignedStaffUserId",
                table: "InboundRequest",
                column: "AssignedStaffUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InboundRequest_User_AssignedStaffUserId",
                table: "InboundRequest",
                column: "AssignedStaffUserId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InboundRequest_User_AssignedStaffUserId",
                table: "InboundRequest");

            migrationBuilder.DropIndex(
                name: "IX_InboundRequest_AssignedStaffUserId",
                table: "InboundRequest");

            migrationBuilder.DropColumn(
                name: "AssignedStaffUserId",
                table: "InboundRequest");
        }
    }
}
