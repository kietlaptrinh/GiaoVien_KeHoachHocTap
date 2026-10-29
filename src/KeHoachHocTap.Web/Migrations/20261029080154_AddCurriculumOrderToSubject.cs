using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeHoachHocTap.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumOrderToSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurriculumOrder",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_CurriculumOrder",
                table: "Subjects",
                column: "CurriculumOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_CurriculumOrder",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "CurriculumOrder",
                table: "Subjects");
        }
    }
}
