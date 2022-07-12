using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22071201 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PropertiesString",
                table: "SeparateThings");

            migrationBuilder.AddColumn<string>(
                name: "JsonObjectString",
                table: "SeparateThings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JsonObjectString",
                table: "SeparateThings");

            migrationBuilder.AddColumn<string>(
                name: "PropertiesString",
                table: "SeparateThings",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
