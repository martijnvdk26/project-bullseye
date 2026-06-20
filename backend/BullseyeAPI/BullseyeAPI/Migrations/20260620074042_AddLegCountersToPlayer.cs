using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BullseyeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLegCountersToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LegsPlayed",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LegsWon",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegsPlayed",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "LegsWon",
                table: "Players");
        }
    }
}
