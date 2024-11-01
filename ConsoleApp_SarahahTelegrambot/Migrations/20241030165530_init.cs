using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsoleApp_SarahahTelegrambot.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblUsers",
                columns: table => new
                {
                    LongSenderChatId = table.Column<long>(type: "bigint", nullable: false),
                    LongLastReceiverChatId = table.Column<long>(type: "bigint", nullable: true),
                    StringSenderChatId = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    RegisterName = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblUsers", x => x.LongSenderChatId);
                });

            migrationBuilder.CreateTable(
                name: "tblMessages",
                columns: table => new
                {
                    SenderMessageId = table.Column<int>(type: "int", nullable: false),
                    ReceiverMessageId = table.Column<int>(type: "int", nullable: false),
                    ReplyToMessageId = table.Column<int>(type: "int", nullable: true),
                    ReceiverChatId = table.Column<long>(type: "bigint", nullable: false),
                    SenderChatId = table.Column<long>(type: "bigint", nullable: false),
                    SendDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TextMessage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblMessages", x => x.SenderMessageId);
                    table.ForeignKey(
                        name: "FK_tblMessages_tblUsers_SenderChatId",
                        column: x => x.SenderChatId,
                        principalTable: "tblUsers",
                        principalColumn: "LongSenderChatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblMessages_SenderChatId",
                table: "tblMessages",
                column: "SenderChatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblMessages");

            migrationBuilder.DropTable(
                name: "tblUsers");
        }
    }
}
