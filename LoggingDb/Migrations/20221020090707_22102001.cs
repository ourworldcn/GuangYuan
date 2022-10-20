using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LoggingDb.Migrations
{
    public partial class _22102001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayOrders",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 64, nullable: false),
                    PayerId = table.Column<string>(maxLength: 64, nullable: true),
                    CreateUtc = table.Column<DateTime>(nullable: false),
                    CurrencyId = table.Column<string>(maxLength: 8, nullable: true),
                    Amount = table.Column<decimal>(nullable: false),
                    OffsetId = table.Column<string>(maxLength: 64, nullable: true),
                    Bank = table.Column<int>(nullable: false),
                    Audit = table.Column<bool>(nullable: false),
                    JsonObjectString = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayOrders", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayOrders");
        }
    }
}
