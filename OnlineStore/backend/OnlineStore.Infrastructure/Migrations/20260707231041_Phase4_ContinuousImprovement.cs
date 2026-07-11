using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_ContinuousImprovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PromptVariantId",
                table: "AiConversations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiConversationAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalConversations = table.Column<int>(type: "int", nullable: false),
                    SuccessfulConversations = table.Column<int>(type: "int", nullable: false),
                    FailedConversations = table.Column<int>(type: "int", nullable: false),
                    AbandonedConversations = table.Column<int>(type: "int", nullable: false),
                    AverageSatisfactionRating = table.Column<double>(type: "float", nullable: false),
                    TotalFeedbackCount = table.Column<int>(type: "int", nullable: false),
                    NegativeFeedbackCount = table.Column<int>(type: "int", nullable: false),
                    AgentPerformanceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopIssuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommonQuestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToolPerformanceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversationAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    MessageId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    FeedbackType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImplicitSignal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FeedbackSource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiFeedbacks_AiConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AiConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiFeedbacks_AiMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "AiMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiImprovementSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SuggestedChange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiImprovementSuggestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiKnowledgeGaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Topic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExampleQuestions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FirstDetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastDetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiKnowledgeGaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiPromptVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TotalConversations = table.Column<int>(type: "int", nullable: false),
                    AverageSatisfaction = table.Column<double>(type: "float", nullable: false),
                    ConversionRate = table.Column<double>(type: "float", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPromptVariants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationAnalyses_AnalysisDate",
                table: "AiConversationAnalyses",
                column: "AnalysisDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiFeedbacks_AgentName",
                table: "AiFeedbacks",
                column: "AgentName");

            migrationBuilder.CreateIndex(
                name: "IX_AiFeedbacks_ConversationId_CreatedAtUtc",
                table: "AiFeedbacks",
                columns: new[] { "ConversationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiFeedbacks_MessageId",
                table: "AiFeedbacks",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_AiFeedbacks_UserId",
                table: "AiFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiImprovementSuggestions_AgentName",
                table: "AiImprovementSuggestions",
                column: "AgentName");

            migrationBuilder.CreateIndex(
                name: "IX_AiImprovementSuggestions_Status",
                table: "AiImprovementSuggestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AiKnowledgeGaps_Status",
                table: "AiKnowledgeGaps",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AiKnowledgeGaps_Topic",
                table: "AiKnowledgeGaps",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptVariants_AgentName_VariantName",
                table: "AiPromptVariants",
                columns: new[] { "AgentName", "VariantName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiConversationAnalyses");

            migrationBuilder.DropTable(
                name: "AiFeedbacks");

            migrationBuilder.DropTable(
                name: "AiImprovementSuggestions");

            migrationBuilder.DropTable(
                name: "AiKnowledgeGaps");

            migrationBuilder.DropTable(
                name: "AiPromptVariants");

            migrationBuilder.DropColumn(
                name: "PromptVariantId",
                table: "AiConversations");
        }
    }
}
