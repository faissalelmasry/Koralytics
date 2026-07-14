using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterTeamId = table.Column<int>(type: "int", nullable: false),
                    TargetTeamId = table.Column<int>(type: "int", nullable: false),
                    RequesterCoachId = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResolvedByCoachId = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MatchId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchRequests_AspNetUsers_RequesterCoachId",
                        column: x => x.RequesterCoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchRequests_AspNetUsers_ResolvedByCoachId",
                        column: x => x.ResolvedByCoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchRequests_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MatchRequests_Teams_RequesterTeamId",
                        column: x => x.RequesterTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchRequests_Teams_TargetTeamId",
                        column: x => x.TargetTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchRequests_CreatedByUserId",
                table: "MatchRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRequests_MatchId",
                table: "MatchRequests",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRequests_RequesterCoachId",
                table: "MatchRequests",
                column: "RequesterCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRequests_RequesterTeamId",
                table: "MatchRequests",
                column: "RequesterTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRequests_ResolvedByCoachId",
                table: "MatchRequests",
                column: "ResolvedByCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRequests_Status",
                table: "MatchRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRequests_TargetTeamId",
                table: "MatchRequests",
                column: "TargetTeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchRequests");
        }
    }
}
