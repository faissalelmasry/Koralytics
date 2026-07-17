using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeAcademyAdminAcademyIdOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the FK index that depends on AcademyId before altering the column
            migrationBuilder.DropIndex(
                name: "IX_AcademyAdmins_AcademyId",
                table: "AcademyAdmins");

            migrationBuilder.AlterColumn<int>(
                name: "AcademyId",
                table: "AcademyAdmins",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Recreate the index to allow NULLs (SQL Server includes NULLs in non-unique indexes)
            migrationBuilder.CreateIndex(
                name: "IX_AcademyAdmins_AcademyId",
                table: "AcademyAdmins",
                column: "AcademyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AcademyAdmins_AcademyId",
                table: "AcademyAdmins");

            migrationBuilder.AlterColumn<int>(
                name: "AcademyId",
                table: "AcademyAdmins",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademyAdmins_AcademyId",
                table: "AcademyAdmins",
                column: "AcademyId");
        }
    }
}
