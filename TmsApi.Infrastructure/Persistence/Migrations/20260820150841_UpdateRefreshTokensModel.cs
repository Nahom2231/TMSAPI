using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmsApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRefreshTokensModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiredAt",
                table: "RefreshTokens",
                newName: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "RefreshTokens",
                newName: "ExpiredAt");
        }
    }
}
