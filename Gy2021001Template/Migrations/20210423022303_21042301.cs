using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21042301 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SlotTemplates",
                table: "SlotTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SlotTemplates",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "SlotTemplates",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "GenusIdString",
                table: "ItemTemplates",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SlotTemplates",
                table: "SlotTemplates",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SlotTemplates",
                table: "SlotTemplates");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "SlotTemplates");

            migrationBuilder.DropColumn(
                name: "GenusIdString",
                table: "ItemTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SlotTemplates",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SlotTemplates",
                table: "SlotTemplates",
                column: "Name");
        }
    }
}
