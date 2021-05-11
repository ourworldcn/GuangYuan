using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gy2021001Template.Migrations
{
    public partial class _21051101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlueprintTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    ChildrenTemplateIdString = table.Column<string>(nullable: true),
                    GId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlueprintTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BpInputItemTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    BlueprintTemplateId = table.Column<Guid>(nullable: false)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BpInputItemTemplate");

            migrationBuilder.DropTable(
                name: "BlueprintTemplates");
        }
    }
}
