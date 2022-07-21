using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22072102 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreeNodes_TemplateId_ExtraDecimal_ExtraString",
                table: "TreeNodes");

            migrationBuilder.DropIndex(
                name: "IX_TreeNodes_TemplateId_ExtraString_ExtraDecimal",
                table: "TreeNodes");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "TreeNodes");

            migrationBuilder.AddColumn<Guid>(
                name: "ExtraGuid",
                table: "TreeNodes",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TreeNodes_ExtraGuid_ExtraDecimal_ExtraString",
                table: "TreeNodes",
                columns: new[] { "ExtraGuid", "ExtraDecimal", "ExtraString" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreeNodes_ExtraGuid_ExtraString_ExtraDecimal",
                table: "TreeNodes",
                columns: new[] { "ExtraGuid", "ExtraString", "ExtraDecimal" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreeNodes_ExtraGuid_ExtraDecimal_ExtraString",
                table: "TreeNodes");

            migrationBuilder.DropIndex(
                name: "IX_TreeNodes_ExtraGuid_ExtraString_ExtraDecimal",
                table: "TreeNodes");

            migrationBuilder.DropColumn(
                name: "ExtraGuid",
                table: "TreeNodes");

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "TreeNodes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TreeNodes_TemplateId_ExtraDecimal_ExtraString",
                table: "TreeNodes",
                columns: new[] { "TemplateId", "ExtraDecimal", "ExtraString" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_TreeNodes_TemplateId_ExtraString_ExtraDecimal",
                table: "TreeNodes",
                columns: new[] { "TemplateId", "ExtraString", "ExtraDecimal" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });
        }
    }
}
