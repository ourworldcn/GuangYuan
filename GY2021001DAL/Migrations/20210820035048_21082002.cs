using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21082002 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FrinedCount",
                table: "CharSpecificExpandProperty",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FrinedMaxCount",
                table: "CharSpecificExpandProperty",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrinedCount",
                table: "CharSpecificExpandProperty");

            migrationBuilder.DropColumn(
                name: "FrinedMaxCount",
                table: "CharSpecificExpandProperty");
        }
    }
}
