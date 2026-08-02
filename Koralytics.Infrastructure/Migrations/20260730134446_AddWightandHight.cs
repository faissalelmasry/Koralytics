using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWightandHight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParentPlayerJoinRequests_AspNetUsers_CreatedByUserId",
                table: "ParentPlayerJoinRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPlayerJoinRequests_AspNetUsers_UpdatedByUserId",
                table: "ParentPlayerJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_ParentPlayerJoinRequests_CreatedByUserId",
                table: "ParentPlayerJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_ParentPlayerJoinRequests_UpdatedByUserId",
                table: "ParentPlayerJoinRequests");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ParentPlayerJoinRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ParentPlayerJoinRequests");

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

            migrationBuilder.CreateIndex(
                name: "IX_ParentPlayerJoinRequests_CreatedById",
                table: "ParentPlayerJoinRequests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPlayerJoinRequests_UpdatedById",
                table: "ParentPlayerJoinRequests",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPlayerJoinRequests_AspNetUsers_CreatedById",
                table: "ParentPlayerJoinRequests",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPlayerJoinRequests_AspNetUsers_UpdatedById",
                table: "ParentPlayerJoinRequests",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParentPlayerJoinRequests_AspNetUsers_CreatedById",
                table: "ParentPlayerJoinRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ParentPlayerJoinRequests_AspNetUsers_UpdatedById",
                table: "ParentPlayerJoinRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Player_HeightCm",
                table: "Players");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Player_WeakFootRating",
                table: "Players");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Player_WeightKg",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_ParentPlayerJoinRequests_CreatedById",
                table: "ParentPlayerJoinRequests");

            migrationBuilder.DropIndex(
                name: "IX_ParentPlayerJoinRequests_UpdatedById",
                table: "ParentPlayerJoinRequests");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Players");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "ParentPlayerJoinRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "ParentPlayerJoinRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentPlayerJoinRequests_CreatedByUserId",
                table: "ParentPlayerJoinRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPlayerJoinRequests_UpdatedByUserId",
                table: "ParentPlayerJoinRequests",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPlayerJoinRequests_AspNetUsers_CreatedByUserId",
                table: "ParentPlayerJoinRequests",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ParentPlayerJoinRequests_AspNetUsers_UpdatedByUserId",
                table: "ParentPlayerJoinRequests",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
