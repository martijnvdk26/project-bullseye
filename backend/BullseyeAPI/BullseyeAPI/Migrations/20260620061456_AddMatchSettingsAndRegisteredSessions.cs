using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BullseyeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchSettingsAndRegisteredSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetLegs",
                table: "GuestSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetSets",
                table: "GuestSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Variant",
                table: "GuestSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "GuestSessionId",
                table: "Games",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "RegisteredSessionId",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RegisteredSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionCode = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Player1Id = table.Column<int>(type: "integer", nullable: false),
                    Player2Id = table.Column<int>(type: "integer", nullable: true),
                    Variant = table.Column<string>(type: "text", nullable: false),
                    TargetSets = table.Column<int>(type: "integer", nullable: false),
                    TargetLegs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisteredSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegisteredSessions_Players_Player1Id",
                        column: x => x.Player1Id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegisteredSessions_Players_Player2Id",
                        column: x => x.Player2Id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_RegisteredSessionId",
                table: "Games",
                column: "RegisteredSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredSessions_Player1Id",
                table: "RegisteredSessions",
                column: "Player1Id");

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredSessions_Player2Id",
                table: "RegisteredSessions",
                column: "Player2Id");

            migrationBuilder.CreateIndex(
                name: "IX_RegisteredSessions_SessionCode",
                table: "RegisteredSessions",
                column: "SessionCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_RegisteredSessions_RegisteredSessionId",
                table: "Games",
                column: "RegisteredSessionId",
                principalTable: "RegisteredSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_RegisteredSessions_RegisteredSessionId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "RegisteredSessions");

            migrationBuilder.DropIndex(
                name: "IX_Games_RegisteredSessionId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "TargetLegs",
                table: "GuestSessions");

            migrationBuilder.DropColumn(
                name: "TargetSets",
                table: "GuestSessions");

            migrationBuilder.DropColumn(
                name: "Variant",
                table: "GuestSessions");

            migrationBuilder.DropColumn(
                name: "RegisteredSessionId",
                table: "Games");

            migrationBuilder.AlterColumn<int>(
                name: "GuestSessionId",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
