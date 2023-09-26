using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugTrackingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketComments_AspNetUsers_UserId",
                table: "TicketComments");

            migrationBuilder.DropIndex(
                name: "IX_TicketComments_UserId",
                table: "TicketComments");

            migrationBuilder.AddColumn<string>(
                name: "BTUserId",
                table: "TicketComments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileContentType",
                table: "TicketAttachments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "TicketAttachments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_BTUserId",
                table: "TicketComments",
                column: "BTUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketComments_AspNetUsers_BTUserId",
                table: "TicketComments",
                column: "BTUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketComments_AspNetUsers_BTUserId",
                table: "TicketComments");

            migrationBuilder.DropIndex(
                name: "IX_TicketComments_BTUserId",
                table: "TicketComments");

            migrationBuilder.DropColumn(
                name: "BTUserId",
                table: "TicketComments");

            migrationBuilder.DropColumn(
                name: "FileContentType",
                table: "TicketAttachments");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "TicketAttachments");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_UserId",
                table: "TicketComments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketComments_AspNetUsers_UserId",
                table: "TicketComments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
