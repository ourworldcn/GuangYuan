using Microsoft.EntityFrameworkCore.Migrations;

namespace GY2021001DAL.Migrations
{
    public partial class _21042301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameItems_GameChar_UserId",
                table: "GameItems");

            migrationBuilder.DropIndex(
                name: "IX_GameItems_UserId",
                table: "GameItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GameItems_UserId",
                table: "GameItems",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameItems_GameChar_UserId",
                table: "GameItems",
                column: "UserId",
                principalTable: "GameChar",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
