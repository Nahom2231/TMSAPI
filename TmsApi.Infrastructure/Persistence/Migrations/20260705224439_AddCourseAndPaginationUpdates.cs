using Microsoft.EntityFrameworkCore.Migrations;
using TmsApi.Infrastructure.Persistence;
#nullable disable

namespace TmsApi.Infrastructure.Migrations;

    /// <inheritdoc />
    public partial class AddCourseAndPaginationUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Students",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");
        }
    }

