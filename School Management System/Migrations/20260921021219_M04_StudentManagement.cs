using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace School_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class M04_StudentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AcademicSessionId = table.Column<int>(type: "int", nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    GroupStream = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RollNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_AcademicSessions_AcademicSessionId",
                        column: x => x.AcademicSessionId,
                        principalTable: "AcademicSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_SchoolId_RollNumber",
                table: "Students",
                columns: new[] { "SchoolId", "RollNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_AcademicSessionId",
                table: "StudentEnrollments",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_SchoolId_AcademicSessionId_ClassName_SectionName",
                table: "StudentEnrollments",
                columns: new[] { "SchoolId", "AcademicSessionId", "ClassName", "SectionName" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_StudentId_AcademicSessionId",
                table: "StudentEnrollments",
                columns: new[] { "StudentId", "AcademicSessionId" });

            migrationBuilder.CreateIndex(
                name: "UX_StudentEnrollment_OneCurrentPerStudent",
                table: "StudentEnrollments",
                column: "StudentId",
                unique: true,
                filter: "[IsCurrent] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_Students_SchoolId_RollNumber",
                table: "Students");
        }
    }
}
