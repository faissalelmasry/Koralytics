using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class positiondeletecascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerPositions_Players_PlayerId",
                table: "PlayerPositions");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerPositions_Players_PlayerId",
                table: "PlayerPositions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerPositions_Players_PlayerId",
                table: "PlayerPositions");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerPositions_Players_PlayerId",
                table: "PlayerPositions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
