using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BullseyeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerNamesToGuestSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Player1Name",
                table: "GuestSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Player2Name",
                table: "GuestSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Player1Name",
                table: "GuestSessions");

            migrationBuilder.DropColumn(
                name: "Player2Name",
                table: "GuestSessions");
        }
    }
}
