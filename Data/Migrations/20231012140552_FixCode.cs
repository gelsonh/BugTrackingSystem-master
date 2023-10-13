using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugTrackingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAttachments_AspNetUsers_BTUserId",
                table: "TicketAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "BTUserId",
                table: "TicketAttachments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAttachments_AspNetUsers_BTUserId",
                table: "TicketAttachments",
                column: "BTUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketAttachments_AspNetUsers_BTUserId",
                table: "TicketAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "BTUserId",
                table: "TicketAttachments",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketAttachments_AspNetUsers_BTUserId",
                table: "TicketAttachments",
                column: "BTUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
