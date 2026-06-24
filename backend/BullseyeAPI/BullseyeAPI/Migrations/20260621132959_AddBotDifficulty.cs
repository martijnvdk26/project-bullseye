using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BullseyeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBotDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BotDifficulty",
                table: "RegisteredSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BotDifficulty",
                table: "GuestSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BotDifficulty",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotDifficulty",
                table: "RegisteredSessions");

            migrationBuilder.DropColumn(
                name: "BotDifficulty",
                table: "GuestSessions");

            migrationBuilder.DropColumn(
                name: "BotDifficulty",
                table: "Games");
        }
    }
}
