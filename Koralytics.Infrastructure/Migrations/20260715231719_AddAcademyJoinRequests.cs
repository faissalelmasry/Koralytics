using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademyJoinRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademyCoachJoinRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    CoachId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyCoachJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademyCoachJoinRequests_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcademyCoachJoinRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcademyCoachJoinRequests_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcademyCoachJoinRequests_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcademyPlayerJoinRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyPlayerJoinRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademyPlayerJoinRequests_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcademyPlayerJoinRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcademyPlayerJoinRequests_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcademyPlayerJoinRequests_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademyCoachJoinRequests_AcademyId",
                table: "AcademyCoachJoinRequests",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyCoachJoinRequests_CoachId",
                table: "AcademyCoachJoinRequests",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyCoachJoinRequests_CreatedByUserId",
                table: "AcademyCoachJoinRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyCoachJoinRequests_UpdatedByUserId",
                table: "AcademyCoachJoinRequests",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyPlayerJoinRequests_AcademyId",
                table: "AcademyPlayerJoinRequests",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyPlayerJoinRequests_CreatedByUserId",
                table: "AcademyPlayerJoinRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyPlayerJoinRequests_PlayerId",
                table: "AcademyPlayerJoinRequests",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyPlayerJoinRequests_UpdatedByUserId",
                table: "AcademyPlayerJoinRequests",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademyCoachJoinRequests");

            migrationBuilder.DropTable(
                name: "AcademyPlayerJoinRequests");
        }
    }
}
