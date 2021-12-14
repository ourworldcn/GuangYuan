using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21121401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExPropertyString",
                table: "GameItems",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "CharType",
                table: "GameChars",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "ExPropertyString",
                table: "GameChars",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId_ExPropertyString",
                table: "GameItems",
                columns: new[] { "TemplateId", "ExPropertyString" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameItems_TemplateId_ExPropertyString",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "ExPropertyString",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "CharType",
                table: "GameChars");

            migrationBuilder.DropColumn(
                name: "ExPropertyString",
                table: "GameChars");
        }
    }
}
