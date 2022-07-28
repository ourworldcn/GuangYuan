using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22072501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreeNodes");

            migrationBuilder.CreateTable(
                name: "VirtualThings",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    JsonObjectString = table.Column<string>(nullable: true),
                    ExtraGuid = table.Column<Guid>(nullable: false),
                    ExtraString = table.Column<string>(maxLength: 64, nullable: true),
                    ExtraDecimal = table.Column<decimal>(nullable: true),
                    ParentId = table.Column<Guid>(nullable: true),
                    Timestamp = table.Column<byte[]>(rowVersion: true, nullable: true),
                    BinaryArray = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualThings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirtualThings_VirtualThings_ParentId",
                        column: x => x.ParentId,
                        principalTable: "VirtualThings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualThings_ParentId",
                table: "VirtualThings",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualThings_ExtraGuid_ExtraDecimal_ExtraString",
                table: "VirtualThings",
                columns: new[] { "ExtraGuid", "ExtraDecimal", "ExtraString" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualThings_ExtraGuid_ExtraString_ExtraDecimal",
                table: "VirtualThings",
                columns: new[] { "ExtraGuid", "ExtraString", "ExtraDecimal" })
                .Annotation("SqlServer:Include", new[] { "ParentId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VirtualThings");

            migrationBuilder.CreateTable(
                name: "TreeNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraDecimal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExtraGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraString = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    JsonObjectString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
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
    }
}
