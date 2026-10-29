using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeHoachHocTap.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCurriculumFieldsToSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Coefficient",
                table: "Subjects",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeBlock",
                table: "Subjects",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Coefficient",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "KnowledgeBlock",
                table: "Subjects");
        }
    }
}
