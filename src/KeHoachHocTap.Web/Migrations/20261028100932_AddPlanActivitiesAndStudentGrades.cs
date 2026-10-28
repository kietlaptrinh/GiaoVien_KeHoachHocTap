using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeHoachHocTap.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanActivitiesAndStudentGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AfterSubjectId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Sessions = table.Column<int>(type: "int", nullable: true),
                    Periods = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanActivities_Subjects_AfterSubjectId",
                        column: x => x.AfterSubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGrades_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentGrades_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanActivities_AfterSubjectId",
                table: "PlanActivities",
                column: "AfterSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanActivities_SortOrder",
                table: "PlanActivities",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGrades_StudentId_SubjectId",
                table: "StudentGrades",
                columns: new[] { "StudentId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGrades_SubjectId",
                table: "StudentGrades",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_SortOrder",
                table: "Students",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentCode",
                table: "Students",
                column: "StudentCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanActivities");

            migrationBuilder.DropTable(
                name: "StudentGrades");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
