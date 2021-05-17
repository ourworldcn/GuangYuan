using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21051701 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlueprintTemplates",
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
                    table.PrimaryKey("PK_BlueprintTemplates", x => x.Id);
                });

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
                name: "BpFormulaTemplate",
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
                    table.PrimaryKey("PK_BptFormulaTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BptFormulaTemplate_BlueprintTemplates_蓝图Id",
                        column: x => x.蓝图Id,
                        principalTable: "BlueprintTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BpItemTemplate",
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
                    table.PrimaryKey("PK_BptfItemTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BptfItemTemplate_BptFormulaTemplate_公式Id",
                        column: x => x.公式Id,
                        principalTable: "BpFormulaTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BptfItemTemplate_公式Id",
                table: "BpItemTemplate",
                column: "公式Id");

            migrationBuilder.CreateIndex(
                name: "IX_BptFormulaTemplate_蓝图Id",
                table: "BpFormulaTemplate",
                column: "蓝图Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BpItemTemplate");

            migrationBuilder.DropTable(
                name: "ItemTemplates");

            migrationBuilder.DropTable(
                name: "BpFormulaTemplate");

            migrationBuilder.DropTable(
                name: "BlueprintTemplates");
        }
    }
}
