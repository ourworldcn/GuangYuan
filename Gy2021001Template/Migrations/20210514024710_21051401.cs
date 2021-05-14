using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21051401 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BpInputItemTemplate");

            migrationBuilder.AddColumn<string>(
                name: "备注",
                table: "ItemTemplates",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "备注",
                table: "BlueprintTemplates",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BptFormulaTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    备注 = table.Column<string>(nullable: true),
                    PropertiesString = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "IX_BptFormulaTemplate_蓝图Id",
                table: "BptFormulaTemplate",
                column: "蓝图Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BptFormulaTemplate");

            migrationBuilder.DropColumn(
                name: "备注",
                table: "ItemTemplates");

            migrationBuilder.DropColumn(
                name: "备注",
                table: "BlueprintTemplates");

            migrationBuilder.CreateTable(
                name: "BpInputItemTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlueprintTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BpInputItemTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BpInputItemTemplate_BlueprintTemplates_BlueprintTemplateId",
                        column: x => x.BlueprintTemplateId,
                        principalTable: "BlueprintTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BpInputItemTemplate_BlueprintTemplateId",
                table: "BpInputItemTemplate",
                column: "BlueprintTemplateId");
        }
    }
}
