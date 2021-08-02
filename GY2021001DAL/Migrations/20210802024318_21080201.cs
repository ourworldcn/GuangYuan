using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21080201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SocialRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertyString = table.Column<string>(nullable: true),
                    LeftId = table.Column<Guid>(nullable: false),
                    RightId = table.Column<Guid>(nullable: false),
                    FriendlinessLToR = table.Column<short>(nullable: false),
                    FriendlinessRToL = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialRelationships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_LeftId",
                table: "SocialRelationships",
                column: "LeftId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_RightId",
                table: "SocialRelationships",
                column: "RightId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SocialRelationships");
        }
    }
}
