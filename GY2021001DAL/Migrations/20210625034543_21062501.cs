using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21062501 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientExtendProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ParentId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: true),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientExtendProperties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameSettings",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    Val = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSettings", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "GameUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TemplateId = table.Column<Guid>(nullable: false),
                    LoginName = table.Column<string>(maxLength: 64, nullable: false),
                    PwdHash = table.Column<byte[]>(nullable: true),
                    Region = table.Column<string>(maxLength: 64, nullable: true),
                    CreateUtc = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameThingBase",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TemplateId = table.Column<Guid>(nullable: false),
                    ClientGutsString = table.Column<string>(nullable: true),
                    CreateUtc = table.Column<DateTime>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    Discriminator = table.Column<string>(nullable: false),
                    GameUserId = table.Column<Guid>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    CurrentDungeonId = table.Column<Guid>(nullable: true),
                    CombatStartUtc = table.Column<DateTime>(nullable: true),
                    Count = table.Column<decimal>(nullable: true),
                    ParentId = table.Column<Guid>(nullable: true),
                    OwnerId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameThingBase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameThingBase_GameUsers_GameUserId",
                        column: x => x.GameUserId,
                        principalTable: "GameUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameThingBase_GameThingBase_ParentId",
                        column: x => x.ParentId,
                        principalTable: "GameThingBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtendProperties",
                columns: table => new
                {
                    ParentId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: false),
                    StringValue = table.Column<string>(maxLength: 256, nullable: true),
                    IntValue = table.Column<int>(nullable: false),
                    DecimalValue = table.Column<decimal>(nullable: false),
                    DoubleValue = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameExtendProperties", x => new { x.ParentId, x.Name });
                    table.ForeignKey(
                        name: "FK_GameExtendProperties_GameThingBase_ParentId",
                        column: x => x.ParentId,
                        principalTable: "GameThingBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientExtendProperties_ParentId",
                table: "ClientExtendProperties",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GameExtendProperties_DecimalValue",
                table: "ExtendProperties",
                column: "DecimalValue");

            migrationBuilder.CreateIndex(
                name: "IX_GameExtendProperties_DoubleValue",
                table: "ExtendProperties",
                column: "DoubleValue");

            migrationBuilder.CreateIndex(
                name: "IX_GameExtendProperties_IntValue",
                table: "ExtendProperties",
                column: "IntValue");

            migrationBuilder.CreateIndex(
                name: "IX_GameExtendProperties_Name",
                table: "ExtendProperties",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_GameExtendProperties_StringValue",
                table: "ExtendProperties",
                column: "StringValue");

            migrationBuilder.CreateIndex(
                name: "IX_GameThingBase_GameUserId",
                table: "GameThingBase",
                column: "GameUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameThingBase_OwnerId",
                table: "GameThingBase",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameThingBase_ParentId",
                table: "GameThingBase",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GameUsers_CreateUtc",
                table: "GameUsers",
                column: "CreateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_GameUsers_LoginName",
                table: "GameUsers",
                column: "LoginName",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientExtendProperties");

            migrationBuilder.DropTable(
                name: "ExtendProperties");

            migrationBuilder.DropTable(
                name: "GameSettings");

            migrationBuilder.DropTable(
                name: "GameThingBase");

            migrationBuilder.DropTable(
                name: "GameUsers");
        }
    }
}
