using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    public partial class _21100801 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "掉落限制");

            migrationBuilder.CreateTable(
                name: "动态属性元数据",
                columns: table => new
                {
                    PName = table.Column<string>(maxLength: 64, nullable: false),
                    FName = table.Column<string>(nullable: true),
                    前缀 = table.Column<bool>(nullable: false),
                    不变 = table.Column<bool>(nullable: false),
                    隐藏 = table.Column<bool>(nullable: false),
                    默认值 = table.Column<string>(nullable: true),
                    索引属性名 = table.Column<string>(nullable: true),
                    备注 = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_动态属性元数据", x => x.PName);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "动态属性元数据");

            migrationBuilder.CreateTable(
                name: "掉落限制",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    关卡Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    组号 = table.Column<int>(type: "int", nullable: false),
                    物品Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    最大掉落数量 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    本组最大掉落数量 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    备注 = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_掉落限制", x => x.Id);
                });
        }
    }
}
