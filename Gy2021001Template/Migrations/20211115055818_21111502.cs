using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _21111502 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChildrenTemplateIdString",
                table: "ShoppingTemplates");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ShoppingTemplates");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChildrenTemplateIdString",
                table: "ShoppingTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ShoppingTemplates",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
