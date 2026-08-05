using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerPhysicalAndArchetypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentFixtures_TournamentGroups_TournamentGroupId",
                table: "TournamentFixtures");

            migrationBuilder.DropIndex(
                name: "IX_TournamentFixtures_TournamentGroupId",
                table: "TournamentFixtures");

            migrationBuilder.DropColumn(
                name: "TournamentGroupId",
                table: "TournamentFixtures");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchetypeLastRevealedAt",
                table: "Players",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "Players",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "Players",
                type: "decimal(18,2)",
                nullable: true);

            // AlterColumn for ParentPlayers.Id (IDENTITY) skipped — SQL Server cannot add IDENTITY to an existing column.

            // Tables AcademyAdminJoinRequests and ParentPlayerJoinRequests already exist in the database.
            /*
            migrationBuilder.CreateTable(
                name: "AcademyAdminJoinRequests", ...
            */

            migrationBuilder.AddCheckConstraint(
                name: "CK_Player_HeightCm",
                table: "Players",
                sql: "[HeightCm] BETWEEN 50 AND 220");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Player_WeakFootRating",
                table: "Players",
                sql: "[WeakFootRating] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Player_WeightKg",
                table: "Players",
                sql: "[WeightKg] BETWEEN 20 AND 150");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademyAdminJoinRequests");

            migrationBuilder.DropTable(
                name: "ParentPlayerJoinRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Player_HeightCm",
                table: "Players");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Player_WeakFootRating",
                table: "Players");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Player_WeightKg",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ArchetypeLastRevealedAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Players");

            migrationBuilder.AddColumn<int>(
                name: "TournamentGroupId",
                table: "TournamentFixtures",
                type: "int",
                nullable: true);

            // AlterColumn reversal for ParentPlayers.Id skipped — matched skip in Up().

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_TournamentGroupId",
                table: "TournamentFixtures",
                column: "TournamentGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentFixtures_TournamentGroups_TournamentGroupId",
                table: "TournamentFixtures",
                column: "TournamentGroupId",
                principalTable: "TournamentGroups",
                principalColumn: "Id");
        }
    }
}
