using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22062101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameItems_TemplateId_Count",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "Count",
                table: "GameItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Count",
                table: "GameItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId_Count",
                table: "GameItems",
                columns: new[] { "TemplateId", "Count" });
        }
    }
}
