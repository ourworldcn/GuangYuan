﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21072903 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdsOfMarkDeleteStream",
                table: "Mails");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "IdsOfMarkDeleteStream",
                table: "Mails",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
