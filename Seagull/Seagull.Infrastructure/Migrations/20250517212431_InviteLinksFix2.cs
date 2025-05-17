using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seagull.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InviteLinksFix2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_IslandInviteLink",
                table: "IslandInviteLink");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "IslandInviteLink",
                newName: "AuthorId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EffectiveTo",
                table: "IslandInviteLink",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EffectiveFrom",
                table: "IslandInviteLink",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone")
                .OldAnnotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<int>(
                name: "IslandId",
                table: "IslandInviteLink",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.AlterColumn<string>(
                name: "AuthorId",
                table: "IslandInviteLink",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_IslandInviteLink",
                table: "IslandInviteLink",
                column: "Content");

            migrationBuilder.CreateIndex(
                name: "IX_IslandInviteLink_AuthorId",
                table: "IslandInviteLink",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_IslandInviteLink_IslandId",
                table: "IslandInviteLink",
                column: "IslandId");

            migrationBuilder.AddForeignKey(
                name: "FK_IslandInviteLink_AspNetUsers_AuthorId",
                table: "IslandInviteLink",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IslandInviteLink_Island_IslandId",
                table: "IslandInviteLink",
                column: "IslandId",
                principalTable: "Island",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IslandInviteLink_AspNetUsers_AuthorId",
                table: "IslandInviteLink");

            migrationBuilder.DropForeignKey(
                name: "FK_IslandInviteLink_Island_IslandId",
                table: "IslandInviteLink");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IslandInviteLink",
                table: "IslandInviteLink");

            migrationBuilder.DropIndex(
                name: "IX_IslandInviteLink_AuthorId",
                table: "IslandInviteLink");

            migrationBuilder.DropIndex(
                name: "IX_IslandInviteLink_IslandId",
                table: "IslandInviteLink");

            migrationBuilder.RenameColumn(
                name: "AuthorId",
                table: "IslandInviteLink",
                newName: "UserId");

            migrationBuilder.AlterColumn<int>(
                name: "IslandId",
                table: "IslandInviteLink",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EffectiveTo",
                table: "IslandInviteLink",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EffectiveFrom",
                table: "IslandInviteLink",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone")
                .Annotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "IslandInviteLink",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_IslandInviteLink",
                table: "IslandInviteLink",
                columns: new[] { "IslandId", "UserId", "EffectiveFrom" });
        }
    }
}
