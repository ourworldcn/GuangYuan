using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22022801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameItems_TemplateId_ExPropertyString_OrderbyDecimal",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "ExPropertyString",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "OrderbyDecimal",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "ExPropertyString",
                table: "GameChars");

            migrationBuilder.DropColumn(
                name: "OrderbyDecimal",
                table: "GameChars");

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraDecimal",
                table: "GameItems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtraString",
                table: "GameItems",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraDecimal",
                table: "GameChars",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtraString",
                table: "GameChars",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId_ExtraDecimal",
                table: "GameItems",
                columns: new[] { "TemplateId", "ExtraDecimal" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId_ExtraString_ExtraDecimal",
                table: "GameItems",
                columns: new[] { "TemplateId", "ExtraString", "ExtraDecimal" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameItems_TemplateId_ExtraDecimal",
                table: "GameItems");

            migrationBuilder.DropIndex(
                name: "IX_GameItems_TemplateId_ExtraString_ExtraDecimal",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "ExtraDecimal",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "ExtraString",
                table: "GameItems");

            migrationBuilder.DropColumn(
                name: "ExtraDecimal",
                table: "GameChars");

            migrationBuilder.DropColumn(
                name: "ExtraString",
                table: "GameChars");

            migrationBuilder.AddColumn<string>(
                name: "ExPropertyString",
                table: "GameItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderbyDecimal",
                table: "GameItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExPropertyString",
                table: "GameChars",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderbyDecimal",
                table: "GameChars",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId_ExPropertyString_OrderbyDecimal",
                table: "GameItems",
                columns: new[] { "TemplateId", "ExPropertyString", "OrderbyDecimal" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });
        }
    }
}
