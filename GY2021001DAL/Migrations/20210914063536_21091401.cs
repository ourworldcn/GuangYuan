using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21091401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameItems_TemplateId",
                table: "GameItems");

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId_Count",
                table: "GameItems",
                columns: new[] { "TemplateId", "Count" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameItems_TemplateId_Count",
                table: "GameItems");

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId",
                table: "GameItems",
                column: "TemplateId");
        }
    }
}
