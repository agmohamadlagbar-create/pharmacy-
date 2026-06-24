using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiConversationToolCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "AiMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "AiMessages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageAtUtc",
                table: "AiConversations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "AiConversations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "AiConversations",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiToolCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    MessageId = table.Column<int>(type: "int", nullable: true),
                    ToolCallId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ArgumentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiToolCalls", x => x.Id);
                    table.CheckConstraint("CK_AiToolCalls_Status", "[Status] IN ('Requested','Succeeded','Failed')");
                    table.ForeignKey(
                        name: "FK_AiToolCalls_AiConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AiConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiToolCalls_AiMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "AiMessages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiMessages_ConversationId_SequenceNumber",
                table: "AiMessages",
                columns: new[] { "ConversationId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiMessages_UserId",
                table: "AiMessages",
                column: "UserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AiMessages_Role",
                table: "AiMessages",
                sql: "[Role] IN ('user','assistant','system','tool')");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_UserId_UpdatedAtUtc",
                table: "AiConversations",
                columns: new[] { "UserId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiToolCalls_ConversationId",
                table: "AiToolCalls",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiToolCalls_ConversationId_StartedAtUtc",
                table: "AiToolCalls",
                columns: new[] { "ConversationId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiToolCalls_ConversationId_ToolCallId",
                table: "AiToolCalls",
                columns: new[] { "ConversationId", "ToolCallId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiToolCalls_MessageId",
                table: "AiToolCalls",
                column: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_AiConversations_AspNetUsers_UserId",
                table: "AiConversations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AiMessages_AspNetUsers_UserId",
                table: "AiMessages",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiConversations_AspNetUsers_UserId",
                table: "AiConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_AiMessages_AspNetUsers_UserId",
                table: "AiMessages");

            migrationBuilder.DropTable(
                name: "AiToolCalls");

            migrationBuilder.DropIndex(
                name: "IX_AiMessages_ConversationId_SequenceNumber",
                table: "AiMessages");

            migrationBuilder.DropIndex(
                name: "IX_AiMessages_UserId",
                table: "AiMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AiMessages_Role",
                table: "AiMessages");

            migrationBuilder.DropIndex(
                name: "IX_AiConversations_UserId_UpdatedAtUtc",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "AiMessages");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AiMessages");

            migrationBuilder.DropColumn(
                name: "LastMessageAtUtc",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "AiConversations");
        }
    }
}
