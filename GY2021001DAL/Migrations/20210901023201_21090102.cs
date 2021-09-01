using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21090102 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_KeyType",
                table: "SocialRelationships",
                column: "KeyType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SocialRelationships_KeyType",
                table: "SocialRelationships");
        }
    }
}
