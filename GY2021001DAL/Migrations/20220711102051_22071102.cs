using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22071102 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SeparateThings_OwnerId",
                table: "SeparateThings",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SeparateThings_TemplateId_ExtraDecimal_ExtraString",
                table: "SeparateThings",
                columns: new[] { "TemplateId", "ExtraDecimal", "ExtraString" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_SeparateThings_TemplateId_ExtraString_ExtraDecimal",
                table: "SeparateThings",
                columns: new[] { "TemplateId", "ExtraString", "ExtraDecimal" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SeparateThings_OwnerId",
                table: "SeparateThings");

            migrationBuilder.DropIndex(
                name: "IX_SeparateThings_TemplateId_ExtraDecimal_ExtraString",
                table: "SeparateThings");

            migrationBuilder.DropIndex(
                name: "IX_SeparateThings_TemplateId_ExtraString_ExtraDecimal",
                table: "SeparateThings");
        }
    }
}
