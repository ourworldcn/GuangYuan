using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    ParentId = table.Column<Guid>(nullable: false),
                    ActionId = table.Column<string>(maxLength: 64, nullable: true),
                    DateTimeUtc = table.Column<DateTime>(nullable: false),
                    Remark = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionRecords", x => x.Id);
                });

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
                    PropertiesString = table.Column<string>(nullable: true),
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
                name: "IdMarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ParentId = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdMarks", x => new { x.Id, x.ParentId });
                });

            migrationBuilder.CreateTable(
                name: "Mails",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    Subject = table.Column<string>(nullable: true),
                    Body = table.Column<string>(nullable: true),
                    CreateUtc = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ObjectId = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    Friendliness = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialRelationships", x => new { x.Id, x.ObjectId });
                });

            migrationBuilder.CreateTable(
                name: "GameThingBase",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    ClientGutsString = table.Column<string>(nullable: true),
                    CreateUtc = table.Column<DateTime>(nullable: false),
                    TemplateId = table.Column<Guid>(nullable: false),
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
                name: "GameMailAttachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    MailId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMailAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameMailAttachment_Mails_MailId",
                        column: x => x.MailId,
                        principalTable: "Mails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailAddress",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DisplayName = table.Column<string>(nullable: true),
                    ThingId = table.Column<Guid>(nullable: false),
                    MailId = table.Column<Guid>(nullable: false),
                    Kind = table.Column<int>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailAddress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailAddress_Mails_MailId",
                        column: x => x.MailId,
                        principalTable: "Mails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    DoubleValue = table.Column<double>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtendProperties", x => new { x.ParentId, x.Name });
                    table.ForeignKey(
                        name: "FK_ExtendProperties_GameThingBase_ParentId",
                        column: x => x.ParentId,
                        principalTable: "GameThingBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ActionId",
                table: "ActionRecords",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_DateTimeUtc",
                table: "ActionRecords",
                column: "DateTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ParentId",
                table: "ActionRecords",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientExtendProperties_ParentId",
                table: "ClientExtendProperties",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtendProperties_DecimalValue",
                table: "ExtendProperties",
                column: "DecimalValue");

            migrationBuilder.CreateIndex(
                name: "IX_ExtendProperties_DoubleValue",
                table: "ExtendProperties",
                column: "DoubleValue");

            migrationBuilder.CreateIndex(
                name: "IX_ExtendProperties_IntValue",
                table: "ExtendProperties",
                column: "IntValue");

            migrationBuilder.CreateIndex(
                name: "IX_ExtendProperties_Name",
                table: "ExtendProperties",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ExtendProperties_StringValue",
                table: "ExtendProperties",
                column: "StringValue");

            migrationBuilder.CreateIndex(
                name: "IX_GameMailAttachment_MailId",
                table: "GameMailAttachment",
                column: "MailId");

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

            migrationBuilder.CreateIndex(
                name: "IX_IdMarks_ParentId",
                table: "IdMarks",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_MailAddress_MailId",
                table: "MailAddress",
                column: "MailId");

            migrationBuilder.CreateIndex(
                name: "IX_MailAddress_ThingId",
                table: "MailAddress",
                column: "ThingId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_Friendliness",
                table: "SocialRelationships",
                column: "Friendliness");

            migrationBuilder.CreateIndex(
                name: "IX_SocialRelationships_ObjectId",
                table: "SocialRelationships",
                column: "ObjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionRecords");

            migrationBuilder.DropTable(
                name: "ClientExtendProperties");

            migrationBuilder.DropTable(
                name: "ExtendProperties");

            migrationBuilder.DropTable(
                name: "GameMailAttachment");

            migrationBuilder.DropTable(
                name: "GameSettings");

            migrationBuilder.DropTable(
                name: "IdMarks");

            migrationBuilder.DropTable(
                name: "MailAddress");

            migrationBuilder.DropTable(
                name: "SocialRelationships");

            migrationBuilder.DropTable(
                name: "GameThingBase");

            migrationBuilder.DropTable(
                name: "Mails");

            migrationBuilder.DropTable(
                name: "GameUsers");
        }
    }
}
