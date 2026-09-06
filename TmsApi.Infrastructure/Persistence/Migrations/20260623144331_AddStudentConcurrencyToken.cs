using Microsoft.EntityFrameworkCore.Migrations;
using TmsApi.Infrastructure.Persistence;
#nullable disable

namespace TmsApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Students",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Students");
        }
    }
}
