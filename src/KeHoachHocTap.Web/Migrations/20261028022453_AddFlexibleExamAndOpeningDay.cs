using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeHoachHocTap.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFlexibleExamAndOpeningDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomExamSlot",
                table: "Subjects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExamMode",
                table: "Subjects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Immediately");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomExamSlot",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "ExamMode",
                table: "Subjects");
        }
    }
}
