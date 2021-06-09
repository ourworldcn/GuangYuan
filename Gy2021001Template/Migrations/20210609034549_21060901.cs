using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21060901 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "掉落限制",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    关卡Id = table.Column<Guid>(nullable: false),
                    物品Id = table.Column<Guid>(nullable: false),
                    组号 = table.Column<int>(nullable: false),
                    最大掉落数量 = table.Column<decimal>(nullable: false),
                    本组最大掉落数量 = table.Column<decimal>(nullable: false),
                    备注 = table.Column<string>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_掉落限制", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "掉落限制");
        }
    }
}
