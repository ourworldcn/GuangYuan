using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22072001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeparateThings");

            migrationBuilder.CreateTable(
                name: "TreeNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    JsonObjectString = table.Column<string>(nullable: true),
                    TemplateId = table.Column<Guid>(nullable: false),
                    ExtraString = table.Column<string>(maxLength: 64, nullable: true),
                    ExtraDecimal = table.Column<decimal>(nullable: true),
                    ParentId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreeNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreeNodes_TreeNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TreeNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreeNodes_ParentId",
                table: "TreeNodes",
                column: "ParentId");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreeNodes");

            migrationBuilder.CreateTable(
                name: "SeparateThings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinaryArray = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ExtraDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExtraString = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PropertiesString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeparateThings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeparateThings_SeparateThings_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SeparateThings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeparateThings_OwnerId",
                table: "SeparateThings",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SeparateThings_ParentId",
                table: "SeparateThings",
                column: "ParentId");

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
    }
}
