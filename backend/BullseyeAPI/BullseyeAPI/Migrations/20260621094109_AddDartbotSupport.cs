using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BullseyeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDartbotSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VsBot",
                table: "RegisteredSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBot",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VsBot",
                table: "GuestSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BotPlayerId",
                table: "Games",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VsBot",
                table: "RegisteredSessions");

            migrationBuilder.DropColumn(
                name: "IsBot",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "VsBot",
                table: "GuestSessions");

            migrationBuilder.DropColumn(
                name: "BotPlayerId",
                table: "Games");
        }
    }
}
