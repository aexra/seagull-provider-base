using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seagull.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InviteLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IslandInviteLink",
                columns: table => new
                {
                    IslandId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsagesMax = table.Column<int>(type: "integer", nullable: true),
                    UsagesCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IslandInviteLink", x => new { x.IslandId, x.UserId, x.EffectiveFrom });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IslandInviteLink");
        }
    }
}
