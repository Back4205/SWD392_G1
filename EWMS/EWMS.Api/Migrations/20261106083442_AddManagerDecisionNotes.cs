using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EWMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerDecisionNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "InboundRequest",
                type: "nvarchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "InboundRequest",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DecisionDate",
                table: "InboundRequest",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "GoodsReceipt",
                type: "nvarchar(255)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không đổi về varchar nếu việc đổi kiểu làm mất ký tự tiếng Việt đã lưu.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM [InboundRequest]
                    WHERE [RejectionReason] IS NOT NULL
                      AND CONVERT(nvarchar(255), CONVERT(varchar(255), [RejectionReason])) COLLATE Latin1_General_100_BIN2
                          <> [RejectionReason] COLLATE Latin1_General_100_BIN2
                )
                THROW 51002, 'Cannot downgrade InboundRequest.RejectionReason without losing Unicode characters.', 1;");

            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "InboundRequest");

            migrationBuilder.DropColumn(
                name: "DecisionDate",
                table: "InboundRequest");

            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "GoodsReceipt");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "InboundRequest",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldNullable: true);
        }
    }
}
