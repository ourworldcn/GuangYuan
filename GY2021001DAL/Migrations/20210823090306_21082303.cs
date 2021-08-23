using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21082303 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SocialRelationships_Friendliness",
                table: "SocialRelationships");

            migrationBuilder.DropColumn(
                name: "Friendliness",
                table: "SocialRelationships");

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_Flag",
                table: "SocialRelationships",
                column: "Flag");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SocialRelationships_Flag",
                table: "SocialRelationships");

            migrationBuilder.AddColumn<short>(
                name: "Friendliness",
                table: "SocialRelationships",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_Friendliness",
                table: "SocialRelationships",
                column: "Friendliness");
        }
    }
}
