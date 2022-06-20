using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _22062001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExtendProperties");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExtendProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ByteArray = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GuidValue = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IntValue = table.Column<int>(type: "int", nullable: true),
                    PropertiesString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StringValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtendProperties", x => new { x.Id, x.Name });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExtendProperties_Name_DecimalValue",
                table: "ExtendProperties",
                columns: new[] { "Name", "DecimalValue" });

            migrationBuilder.CreateIndex(
                name: "IX_ExtendProperties_Name_IntValue",
                table: "ExtendProperties",
                columns: new[] { "Name", "IntValue" });
        }
    }
}
