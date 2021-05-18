using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21051802 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    备注 = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    PropertiesString = table.Column<string>(nullable: true),
                    ChildrenTemplateIdString = table.Column<string>(nullable: true),
                    GId = table.Column<int>(nullable: true),
                    GenusIdString = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "蓝图",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    备注 = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    PropertiesString = table.Column<string>(nullable: true),
                    ChildrenTemplateIdString = table.Column<string>(nullable: true),
                    GId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_蓝图", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "公式",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    备注 = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    PropertiesString = table.Column<string>(nullable: true),
                    ChildrenTemplateIdString = table.Column<string>(nullable: true),
                    蓝图Id = table.Column<Guid>(nullable: false),
                    序号 = table.Column<int>(nullable: false),
                    命中概率 = table.Column<string>(nullable: true),
                    命中并继续 = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_公式", x => x.Id);
                    table.ForeignKey(
                        name: "FK_公式_蓝图_蓝图Id",
                        column: x => x.蓝图Id,
                        principalTable: "蓝图",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "物料",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    备注 = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    PropertiesString = table.Column<string>(nullable: true),
                    ChildrenTemplateIdString = table.Column<string>(nullable: true),
                    公式Id = table.Column<Guid>(nullable: false),
                    条件属性 = table.Column<string>(nullable: true),
                    增量上限 = table.Column<string>(nullable: true),
                    增量下限 = table.Column<string>(nullable: true),
                    增量概率 = table.Column<string>(nullable: true),
                    增量取整 = table.Column<bool>(nullable: false),
                    属性更改 = table.Column<string>(nullable: true),
                    新建物品否 = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_物料", x => x.Id);
                    table.ForeignKey(
                        name: "FK_物料_公式_公式Id",
                        column: x => x.公式Id,
                        principalTable: "公式",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_公式_蓝图Id",
                table: "公式",
                column: "蓝图Id");

            migrationBuilder.CreateIndex(
                name: "IX_物料_公式Id",
                table: "物料",
                column: "公式Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemTemplates");

            migrationBuilder.DropTable(
                name: "物料");

            migrationBuilder.DropTable(
                name: "公式");

            migrationBuilder.DropTable(
                name: "蓝图");
        }
    }
}
