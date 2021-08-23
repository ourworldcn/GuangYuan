using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21082302 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GameEntityRelationshipBase",
                table: "GameEntityRelationshipBase");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "GameEntityRelationshipBase");

            migrationBuilder.RenameTable(
                name: "GameEntityRelationshipBase",
                newName: "SocialRelationships");

            migrationBuilder.RenameIndex(
                name: "IX_GameEntityRelationshipBase_PropertyString",
                table: "SocialRelationships",
                newName: "IX_SocialRelationships_PropertyString");

            migrationBuilder.RenameIndex(
                name: "IX_GameEntityRelationshipBase_Friendliness",
                table: "SocialRelationships",
                newName: "IX_SocialRelationships_Friendliness");

            migrationBuilder.AlterColumn<short>(
                name: "Friendliness",
                table: "SocialRelationships",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships",
                columns: new[] { "Id", "Id2", "Flag" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships");

            migrationBuilder.RenameTable(
                name: "SocialRelationships",
                newName: "GameEntityRelationshipBase");

            migrationBuilder.RenameIndex(
                name: "IX_SocialRelationships_PropertyString",
                table: "GameEntityRelationshipBase",
                newName: "IX_GameEntityRelationshipBase_PropertyString");

            migrationBuilder.RenameIndex(
                name: "IX_SocialRelationships_Friendliness",
                table: "GameEntityRelationshipBase",
                newName: "IX_GameEntityRelationshipBase_Friendliness");

            migrationBuilder.AlterColumn<short>(
                name: "Friendliness",
                table: "GameEntityRelationshipBase",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(short));

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "GameEntityRelationshipBase",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameEntityRelationshipBase",
                table: "GameEntityRelationshipBase",
                columns: new[] { "Id", "Id2", "Flag" });
        }
    }
}
