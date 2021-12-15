using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21121501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharSpecificExpandProperty");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharSpecificExpandProperty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharLevel = table.Column<int>(type: "int", nullable: false),
                    FrinedCount = table.Column<int>(type: "int", nullable: false),
                    FrinedMaxCount = table.Column<int>(type: "int", nullable: false),
                    LastLogoutUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPvpScore = table.Column<int>(type: "int", nullable: false),
                    PropertiesString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PveCScore = table.Column<int>(type: "int", nullable: false),
                    PveTScore = table.Column<int>(type: "int", nullable: false),
                    PvpScore = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharSpecificExpandProperty", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharSpecificExpandProperty_GameChars_Id",
                        column: x => x.Id,
                        principalTable: "GameChars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharSpecificExpandProperty_LastLogoutUtc",
                table: "CharSpecificExpandProperty",
                column: "LastLogoutUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CharSpecificExpandProperty_PveCScore",
                table: "CharSpecificExpandProperty",
                column: "PveCScore");

            migrationBuilder.CreateIndex(
                name: "IX_CharSpecificExpandProperty_PveTScore",
                table: "CharSpecificExpandProperty",
                column: "PveTScore");

            migrationBuilder.CreateIndex(
                name: "IX_CharSpecificExpandProperty_PvpScore",
                table: "CharSpecificExpandProperty",
                column: "PvpScore");

            migrationBuilder.CreateIndex(
                name: "IX_CharSpecificExpandProperty_State",
                table: "CharSpecificExpandProperty",
                column: "State");
        }
    }
}
