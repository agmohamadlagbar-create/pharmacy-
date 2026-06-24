using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiConversationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "State_AwaitingConfirmation",
                table: "AiConversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "State_PendingAction",
                table: "AiConversations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State_PendingProductId",
                table: "AiConversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State_PendingProductName",
                table: "AiConversations",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State_PendingQuantity",
                table: "AiConversations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State_AwaitingConfirmation",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "State_PendingAction",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "State_PendingProductId",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "State_PendingProductName",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "State_PendingQuantity",
                table: "AiConversations");
        }
    }
}
