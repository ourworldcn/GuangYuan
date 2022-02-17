using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22021701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameItems_Count",
                table: "GameItems");

            migrationBuilder.DropIndex(
                name: "IX_GameItems_TemplateId_ExPropertyString",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "ClientGutsString",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "CreateUtc",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "ClientGutsString",
                table: "GameChars");

            migrationBuilder.AddColumn<byte[]>(
                name: "BinaryArray",
                table: "GameItems",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderbyDecimal",
                table: "GameItems",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "BinaryArray",
                table: "GameChars",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderbyDecimal",
                table: "GameChars",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId_ExPropertyString_OrderbyDecimal",
                table: "GameItems",
                columns: new[] { "TemplateId", "ExPropertyString", "OrderbyDecimal" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameItems_TemplateId_ExPropertyString_OrderbyDecimal",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "BinaryArray",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "OrderbyDecimal",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "BinaryArray",
                table: "GameChars");

            migrationBuilder.DropColumn(
                name: "OrderbyDecimal",
                table: "GameChars");

            migrationBuilder.AddColumn<string>(
                name: "ClientString",
                table: "GameItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateUtc",
                table: "GameItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ClientString",
                table: "GameChars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_Count",
                table: "GameItems",
                column: "Count");

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId_ExPropertyString",
                table: "GameItems",
                columns: new[] { "TemplateId", "ExPropertyString" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });
        }
    }
}
