using Microsoft.EntityFrameworkCore.Migrations;

namespace GY2021001DAL.Migrations
{
    public partial class _21042302 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GameItems_UserId",
                table: "GameItems",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameItems_UserId",
                table: "GameItems");
        }
    }
}
