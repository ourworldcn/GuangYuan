using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _21120301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "物料",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "蓝图",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "公式",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "ItemTemplates",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Script",
                table: "物料");

            migrationBuilder.DropColumn(
                name: "Script",
                table: "蓝图");

            migrationBuilder.DropColumn(
                name: "Script",
                table: "公式");

            migrationBuilder.DropColumn(
                name: "Script",
                table: "ItemTemplates");
        }
    }
}
