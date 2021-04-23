using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21042201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ItemTemplates",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GId",
                table: "ItemTemplates",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SlotTemplates",
                columns: table => new
                {
                    Name = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotTemplates", x => x.Name);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlotTemplates");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ItemTemplates");

            migrationBuilder.DropColumn(
                name: "GId",
                table: "ItemTemplates");
        }
    }
}
