using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21051402 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BptfItemTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    备注 = table.Column<string>(nullable: true),
                    PropertiesString = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    ChildrenTemplateIdString = table.Column<string>(nullable: true),
                    公式Id = table.Column<Guid>(nullable: false),
                    FormulaTemplateId = table.Column<Guid>(nullable: true),
                    条件属性 = table.Column<string>(nullable: true),
                    增量上限 = table.Column<string>(nullable: true),
                    增量下限 = table.Column<string>(nullable: true),
                    增量概率 = table.Column<string>(nullable: true),
                    增量取整 = table.Column<bool>(nullable: false),
                    属性更改 = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BptfItemTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BptfItemTemplate_BptFormulaTemplate_FormulaTemplateId",
                        column: x => x.FormulaTemplateId,
                        principalTable: "BptFormulaTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BptfItemTemplate_FormulaTemplateId",
                table: "BptfItemTemplates",
                column: "FormulaTemplateId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BptfItemTemplates");
        }
    }
}
