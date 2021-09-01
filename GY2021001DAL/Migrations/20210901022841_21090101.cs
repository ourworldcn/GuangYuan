using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21090101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships");

            migrationBuilder.AlterColumn<int>(
                name: "Flag",
                table: "SocialRelationships",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "KeyType",
                table: "SocialRelationships",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships",
                columns: new[] { "Id", "Id2", "KeyType" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships");

            migrationBuilder.DropColumn(
                name: "KeyType",
                table: "SocialRelationships");

            migrationBuilder.AlterColumn<long>(
                name: "Flag",
                table: "SocialRelationships",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AddPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships",
                columns: new[] { "Id", "Id2", "Flag" });
        }
    }
}
