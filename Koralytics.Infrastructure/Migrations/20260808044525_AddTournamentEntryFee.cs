using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentEntryFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EntryFee",
                table: "Tournaments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tournament_EntryFee",
                table: "Tournaments",
                sql: "[EntryFee] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tournament_EntryFee",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "EntryFee",
                table: "Tournaments");
        }
    }
}
