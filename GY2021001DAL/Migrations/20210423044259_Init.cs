﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GY2021001DAL.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "GameChar",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TemplateId = table.Column<Guid>(nullable: false),
                    CreateUtc = table.Column<DateTime>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    GameUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameChar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameChar_GameUsers_GameUserId",
                        column: x => x.GameUserId,
                        principalTable: "GameUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TemplateId = table.Column<Guid>(nullable: false),
                    CreateUtc = table.Column<DateTime>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    Count = table.Column<decimal>(nullable: true),
                    ParentId = table.Column<Guid>(nullable: true),
                    UserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameItems_GameItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "GameItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameItems_GameChar_UserId",
                        column: x => x.UserId,
                        principalTable: "GameChar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameChar_GameUserId",
                table: "GameChar",
                column: "GameUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_ParentId",
                table: "GameItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_UserId",
                table: "GameItems",
                column: "UserId");

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
                name: "GameItems");

            migrationBuilder.DropTable(
                name: "GameSettings");

            migrationBuilder.DropTable(
                name: "GameChar");

            migrationBuilder.DropTable(
                name: "GameUsers");
        }
    }
}