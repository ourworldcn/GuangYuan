using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21042302 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlotTemplates");

            migrationBuilder.AlterColumn<int>(
                name: "GId",
                table: "ItemTemplates",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<string>(
                name: "SlotTemplateIdsString",
                table: "ItemTemplates",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlotTemplateIdsString",
                table: "ItemTemplates");

            migrationBuilder.AlterColumn<int>(
                name: "GId",
                table: "ItemTemplates",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "SlotTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotTemplates", x => x.Id);
                });
        }
    }
}
