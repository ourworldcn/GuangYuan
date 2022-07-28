using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22071101 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeparateThings",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    TemplateId = table.Column<Guid>(nullable: false),
                    ExtraString = table.Column<string>(maxLength: 64, nullable: true),
                    ExtraDecimal = table.Column<decimal>(nullable: true),
                    BinaryArray = table.Column<byte[]>(nullable: true),
                    Timestamp = table.Column<byte[]>(nullable: true),
                    ParentId = table.Column<Guid>(nullable: true),
                    OwnerId = table.Column<Guid>(nullable: true)
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
                name: "IX_SeparateThings_ParentId",
                table: "SeparateThings",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeparateThings");
        }
    }
}
