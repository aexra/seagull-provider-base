using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seagull.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InviteLinksFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "IslandInviteLink",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "IslandInviteLink");
        }
    }
}
