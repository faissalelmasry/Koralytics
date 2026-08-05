using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAIReportStatusAndGeneratedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AIReports",
                type: "int",
                nullable: false,
                defaultValue: 1); // 1 = AIReportStatus.Pending

            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAt",
                table: "AIReports",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "AIReports");

            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "AIReports");
        }
    }
}
