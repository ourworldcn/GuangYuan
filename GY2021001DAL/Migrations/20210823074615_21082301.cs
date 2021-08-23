using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GuangYuan.GY001.UserDb.Migrations
{
    public partial class _21082301 : Migration
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
                name: "GameEntityRelationshipBase",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Id2 = table.Column<Guid>(nullable: false),
                    Flag = table.Column<long>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    PropertyString = table.Column<string>(maxLength: 64, nullable: true),
                    Discriminator = table.Column<string>(nullable: false),
                    Friendliness = table.Column<short>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEntityRelationshipBase", x => new { x.Id, x.Id2, x.Flag });
                });

            migrationBuilder.CreateTable(
                name: "GameItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    ClientGutsString = table.Column<string>(nullable: true),
                    CreateUtc = table.Column<DateTime>(nullable: false),
                    TemplateId = table.Column<Guid>(nullable: false),
                    Count = table.Column<decimal>(nullable: true),
                    ParentId = table.Column<Guid>(nullable: true),
                    OwnerId = table.Column<Guid>(nullable: true)
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
                name: "GameChars",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    ClientGutsString = table.Column<string>(nullable: true),
                    CreateUtc = table.Column<DateTime>(nullable: false),
                    TemplateId = table.Column<Guid>(nullable: false),
                    GameUserId = table.Column<Guid>(nullable: false),
                    DisplayName = table.Column<string>(maxLength: 64, nullable: true),
                    CurrentDungeonId = table.Column<Guid>(nullable: true),
                    CombatStartUtc = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameChars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameChars_GameUsers_GameUserId",
                        column: x => x.GameUserId,
                        principalTable: "GameUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    ThingId = table.Column<Guid>(nullable: false),
                    MailId = table.Column<Guid>(nullable: false),
                    Kind = table.Column<int>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailAddresses_Mails_MailId",
                        column: x => x.MailId,
                        principalTable: "Mails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailAttachmentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    MailId = table.Column<Guid>(nullable: false),
                    ReceivedCharIds = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailAttachmentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailAttachmentes_Mails_MailId",
                        column: x => x.MailId,
                        principalTable: "Mails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharSpecificExpandProperty",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PropertiesString = table.Column<string>(nullable: true),
                    CharLevel = table.Column<int>(nullable: false),
                    PvpScore = table.Column<int>(nullable: false),
                    LastPvpScore = table.Column<int>(nullable: false),
                    PveTScore = table.Column<int>(nullable: false),
                    PveCScore = table.Column<int>(nullable: false),
                    State = table.Column<string>(maxLength: 64, nullable: true),
                    LastLogoutUtc = table.Column<DateTime>(nullable: false),
                    FrinedMaxCount = table.Column<int>(nullable: false),
                    FrinedCount = table.Column<int>(nullable: false)
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
                    Text = table.Column<string>(nullable: true),
                    DateTimeValue = table.Column<DateTime>(nullable: false),
                    GameCharId = table.Column<Guid>(nullable: true),
                    GameItemId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtendProperties", x => new { x.ParentId, x.Name });
                    table.ForeignKey(
                        name: "FK_ExtendProperties_GameChars_GameCharId",
                        column: x => x.GameCharId,
                        principalTable: "GameChars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExtendProperties_GameItems_GameItemId",
                        column: x => x.GameItemId,
                        principalTable: "GameItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ActionId",
                table: "ActionRecords",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_ParentId_ActionId",
                table: "ActionRecords",
                columns: new[] { "ParentId", "ActionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRecords_DateTimeUtc_ParentId_ActionId",
                table: "ActionRecords",
                columns: new[] { "DateTimeUtc", "ParentId", "ActionId" });

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

            migrationBuilder.CreateIndex(
                name: "IX_ExtendProperties_GameCharId",
                table: "ExtendProperties",
                column: "GameCharId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtendProperties_GameItemId",
                table: "ExtendProperties",
                column: "GameItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GameChars_DisplayName",
                table: "GameChars",
                column: "DisplayName",
                unique: true,
                filter: "[DisplayName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GameChars_GameUserId",
                table: "GameChars",
                column: "GameUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameEntityRelationshipBase_Friendliness",
                table: "GameEntityRelationshipBase",
                column: "Friendliness");

            migrationBuilder.CreateIndex(
                name: "IX_GameEntityRelationshipBase_PropertyString",
                table: "GameEntityRelationshipBase",
                column: "PropertyString");

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_OwnerId",
                table: "GameItems",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_ParentId",
                table: "GameItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GameItems_TemplateId",
                table: "GameItems",
                column: "TemplateId");

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
                name: "IX_MailAddresses_MailId",
                table: "MailAddresses",
                column: "MailId");

            migrationBuilder.CreateIndex(
                name: "IX_MailAddresses_ThingId",
                table: "MailAddresses",
                column: "ThingId");

            migrationBuilder.CreateIndex(
                name: "IX_MailAttachmentes_MailId",
                table: "MailAttachmentes",
                column: "MailId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionRecords");

            migrationBuilder.DropTable(
                name: "CharSpecificExpandProperty");

            migrationBuilder.DropTable(
                name: "ExtendProperties");

            migrationBuilder.DropTable(
                name: "GameEntityRelationshipBase");

            migrationBuilder.DropTable(
                name: "GameSettings");

            migrationBuilder.DropTable(
                name: "MailAddresses");

            migrationBuilder.DropTable(
                name: "MailAttachmentes");

            migrationBuilder.DropTable(
                name: "GameChars");

            migrationBuilder.DropTable(
                name: "GameItems");

            migrationBuilder.DropTable(
                name: "Mails");

            migrationBuilder.DropTable(
                name: "GameUsers");
        }
    }
}
