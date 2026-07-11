using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoryAndAbTesting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExperimentEndedAtUtc",
                table: "AiPromptVariants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperimentName",
                table: "AiPromptVariants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExperimentStartedAtUtc",
                table: "AiPromptVariants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuccessfulOutcomes",
                table: "AiPromptVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalOutcomes",
                table: "AiPromptVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TrafficPercentage",
                table: "AiPromptVariants",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "WinnerDecision",
                table: "AiPromptVariants",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiEpisodicMemories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    EpisodeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SentimentAtEnd = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    KeyEntitiesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Importance = table.Column<double>(type: "float", nullable: false),
                    EpisodeDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEpisodicMemories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiEpisodicMemories_AiConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AiConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiLongTermMemories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MemoryType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceConversationSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceConversationId = table.Column<int>(type: "int", nullable: false),
                    Importance = table.Column<double>(type: "float", nullable: false),
                    AccessCount = table.Column<int>(type: "int", nullable: false),
                    LastAccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiLongTermMemories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiShopContexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ShopProfileId = table.Column<int>(type: "int", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PreferredCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ShopPoliciesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ReportingPreferencesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomInstructionsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiShopContexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiUserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PreferenceKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreferenceValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUserPreferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptVariants_AgentName_IsActive",
                table: "AiPromptVariants",
                columns: new[] { "AgentName", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptVariants_ExperimentName",
                table: "AiPromptVariants",
                column: "ExperimentName");

            migrationBuilder.CreateIndex(
                name: "IX_AiEpisodicMemories_ConversationId",
                table: "AiEpisodicMemories",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEpisodicMemories_UserId",
                table: "AiEpisodicMemories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEpisodicMemories_UserId_EpisodeType",
                table: "AiEpisodicMemories",
                columns: new[] { "UserId", "EpisodeType" });

            migrationBuilder.CreateIndex(
                name: "IX_AiLongTermMemories_ExpiresAtUtc",
                table: "AiLongTermMemories",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiLongTermMemories_UserId",
                table: "AiLongTermMemories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiLongTermMemories_UserId_MemoryType",
                table: "AiLongTermMemories",
                columns: new[] { "UserId", "MemoryType" });

            migrationBuilder.CreateIndex(
                name: "IX_AiShopContexts_UserId",
                table: "AiShopContexts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiUserPreferences_UserId_PreferenceKey",
                table: "AiUserPreferences",
                columns: new[] { "UserId", "PreferenceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiEpisodicMemories");

            migrationBuilder.DropTable(
                name: "AiLongTermMemories");

            migrationBuilder.DropTable(
                name: "AiShopContexts");

            migrationBuilder.DropTable(
                name: "AiUserPreferences");

            migrationBuilder.DropIndex(
                name: "IX_AiPromptVariants_AgentName_IsActive",
                table: "AiPromptVariants");

            migrationBuilder.DropIndex(
                name: "IX_AiPromptVariants_ExperimentName",
                table: "AiPromptVariants");

            migrationBuilder.DropColumn(
                name: "ExperimentEndedAtUtc",
                table: "AiPromptVariants");

            migrationBuilder.DropColumn(
                name: "ExperimentName",
                table: "AiPromptVariants");

            migrationBuilder.DropColumn(
                name: "ExperimentStartedAtUtc",
                table: "AiPromptVariants");

            migrationBuilder.DropColumn(
                name: "SuccessfulOutcomes",
                table: "AiPromptVariants");

            migrationBuilder.DropColumn(
                name: "TotalOutcomes",
                table: "AiPromptVariants");

            migrationBuilder.DropColumn(
                name: "TrafficPercentage",
                table: "AiPromptVariants");

            migrationBuilder.DropColumn(
                name: "WinnerDecision",
                table: "AiPromptVariants");
        }
    }
}
