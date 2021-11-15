using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _21111503 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ValidPeriod",
                table: "ShoppingTemplates",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ValidPeriod",
                table: "ShoppingTemplates",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
