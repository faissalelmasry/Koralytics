using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsHomeSideToMatchLineupAndEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchLineups_MatchId_PlayerId_TeamId",
                table: "MatchLineups");

            migrationBuilder.AddColumn<bool>(
                name: "IsHomeSide",
                table: "MatchLineups",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHomeSide",
                table: "MatchEvents",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_MatchId_PlayerId_TeamId",
                table: "MatchLineups",
                columns: new[] { "MatchId", "PlayerId", "TeamId" },
                unique: true,
                filter: "[IsHomeSide] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_MatchId_PlayerId_TeamId_IsHomeSide",
                table: "MatchLineups",
                columns: new[] { "MatchId", "PlayerId", "TeamId", "IsHomeSide" },
                unique: true,
                filter: "[IsHomeSide] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchLineups_MatchId_PlayerId_TeamId_IsHomeSide",
                table: "MatchLineups");

            migrationBuilder.DropIndex(
                name: "IX_MatchLineups_MatchId_PlayerId_TeamId",
                table: "MatchLineups");

            migrationBuilder.DropColumn(
                name: "IsHomeSide",
                table: "MatchLineups");

            migrationBuilder.DropColumn(
                name: "IsHomeSide",
                table: "MatchEvents");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_MatchId_PlayerId_TeamId",
                table: "MatchLineups",
                columns: new[] { "MatchId", "PlayerId", "TeamId" },
                unique: true);
        }
    }
}
