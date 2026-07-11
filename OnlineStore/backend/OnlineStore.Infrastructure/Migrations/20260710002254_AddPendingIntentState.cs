using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingIntentState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "State_LastShoppingIntentJson",
                table: "AiConversations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State_LastSuccessfulToolResult",
                table: "AiConversations",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State_PendingIntentJson",
                table: "AiConversations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State_SelectedProductId",
                table: "AiConversations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State_SelectedStoreName",
                table: "AiConversations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State_LastShoppingIntentJson",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "State_LastSuccessfulToolResult",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "State_PendingIntentJson",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "State_SelectedProductId",
                table: "AiConversations");

            migrationBuilder.DropColumn(
                name: "State_SelectedStoreName",
                table: "AiConversations");
        }
    }
}
