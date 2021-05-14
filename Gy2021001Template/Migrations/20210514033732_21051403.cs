using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21051403 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BptfItemTemplate_BptFormulaTemplate_FormulaTemplateId",
                table: "BptfItemTemplates");

            migrationBuilder.DropIndex(
                name: "IX_BptfItemTemplate_FormulaTemplateId",
                table: "BptfItemTemplates");

            migrationBuilder.DropColumn(
                name: "FormulaTemplateId",
                table: "BptfItemTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_BptfItemTemplate_公式Id",
                table: "BptfItemTemplates",
                column: "公式Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BptfItemTemplate_BptFormulaTemplate_公式Id",
                table: "BptfItemTemplates",
                column: "公式Id",
                principalTable: "BptFormulaTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BptfItemTemplate_BptFormulaTemplate_公式Id",
                table: "BptfItemTemplates");

            migrationBuilder.DropIndex(
                name: "IX_BptfItemTemplate_公式Id",
                table: "BptfItemTemplates");

            migrationBuilder.AddColumn<Guid>(
                name: "FormulaTemplateId",
                table: "BptfItemTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BptfItemTemplate_FormulaTemplateId",
                table: "BptfItemTemplates",
                column: "FormulaTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_BptfItemTemplate_BptFormulaTemplate_FormulaTemplateId",
                table: "BptfItemTemplates",
                column: "FormulaTemplateId",
                principalTable: "BptFormulaTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
