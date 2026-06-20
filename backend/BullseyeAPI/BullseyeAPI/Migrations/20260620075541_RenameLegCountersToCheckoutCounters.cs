using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BullseyeAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameLegCountersToCheckoutCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LegsWon",
                table: "Players",
                newName: "CheckoutHits");

            migrationBuilder.RenameColumn(
                name: "LegsPlayed",
                table: "Players",
                newName: "CheckoutAttempts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CheckoutHits",
                table: "Players",
                newName: "LegsWon");

            migrationBuilder.RenameColumn(
                name: "CheckoutAttempts",
                table: "Players",
                newName: "LegsPlayed");
        }
    }
}
