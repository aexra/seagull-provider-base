using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seagull.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Island_IslandId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IslandId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IslandId",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IslandId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IslandId",
                table: "AspNetUsers",
                column: "IslandId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Island_IslandId",
                table: "AspNetUsers",
                column: "IslandId",
                principalTable: "Island",
                principalColumn: "Id");
        }
    }
}
