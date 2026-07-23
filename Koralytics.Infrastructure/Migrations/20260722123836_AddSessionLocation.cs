using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "DrillSessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "DrillSessions");
        }
    }
}
