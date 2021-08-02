using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21080202 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships");

            migrationBuilder.DropIndex(
                name: "IX_SocialRelationships_LeftId",
                table: "SocialRelationships");

            migrationBuilder.DropIndex(
                name: "IX_SocialRelationships_RightId",
                table: "SocialRelationships");

            migrationBuilder.DropColumn(
                name: "FriendlinessLToR",
                table: "SocialRelationships");

            migrationBuilder.DropColumn(
                name: "FriendlinessRToL",
                table: "SocialRelationships");

            migrationBuilder.DropColumn(
                name: "LeftId",
                table: "SocialRelationships");

            migrationBuilder.DropColumn(
                name: "RightId",
                table: "SocialRelationships");

            migrationBuilder.AddColumn<Guid>(
                name: "ObjectId",
                table: "SocialRelationships",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<short>(
                name: "Friendliness",
                table: "SocialRelationships",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships",
                columns: new[] { "Id", "ObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_Friendliness",
                table: "SocialRelationships",
                column: "Friendliness");

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_ObjectId",
                table: "SocialRelationships",
                column: "ObjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships");

            migrationBuilder.DropIndex(
                name: "IX_SocialRelationships_Friendliness",
                table: "SocialRelationships");

            migrationBuilder.DropIndex(
                name: "IX_SocialRelationships_ObjectId",
                table: "SocialRelationships");

            migrationBuilder.DropColumn(
                name: "ObjectId",
                table: "SocialRelationships");

            migrationBuilder.DropColumn(
                name: "Friendliness",
                table: "SocialRelationships");

            migrationBuilder.AddColumn<short>(
                name: "FriendlinessLToR",
                table: "SocialRelationships",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "FriendlinessRToL",
                table: "SocialRelationships",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<Guid>(
                name: "LeftId",
                table: "SocialRelationships",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RightId",
                table: "SocialRelationships",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_SocialRelationships",
                table: "SocialRelationships",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_LeftId",
                table: "SocialRelationships",
                column: "LeftId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_RightId",
                table: "SocialRelationships",
                column: "RightId");
        }
    }
}
