using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _22040601 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ItemTemplateId",
                table: "ShoppingTemplates",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ItemTemplateId",
                table: "ShoppingTemplates",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);
        }
    }
}
