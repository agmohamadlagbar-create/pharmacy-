using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAiAndTelegram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiConversationAnalyses");

            migrationBuilder.DropTable(
                name: "AiEpisodicMemories");

            migrationBuilder.DropTable(
                name: "AiFeedbacks");

            migrationBuilder.DropTable(
                name: "AiImprovementSuggestions");

            migrationBuilder.DropTable(
                name: "AiKnowledgeGaps");

            migrationBuilder.DropTable(
                name: "AiLongTermMemories");

            migrationBuilder.DropTable(
                name: "AiPromptVariants");

            migrationBuilder.DropTable(
                name: "AiShopContexts");

            migrationBuilder.DropTable(
                name: "AiToolCalls");

            migrationBuilder.DropTable(
                name: "AiUserPreferences");

            migrationBuilder.DropTable(
                name: "BusinessAdvisorMemories");

            migrationBuilder.DropTable(
                name: "BusinessAdvisorReports");

            migrationBuilder.DropTable(
                name: "BusinessRecommendations");

            migrationBuilder.DropTable(
                name: "StoreTelegramNotificationSettings");

            migrationBuilder.DropTable(
                name: "StoreTelegramSettings");

            migrationBuilder.DropTable(
                name: "AiMessages");

            migrationBuilder.DropTable(
                name: "BusinessGoals");

            migrationBuilder.DropTable(
                name: "AiConversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiConversationAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AbandonedConversations = table.Column<int>(type: "int", nullable: false),
                    AgentPerformanceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AverageSatisfactionRating = table.Column<double>(type: "float", nullable: false),
                    CommonQuestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FailedConversations = table.Column<int>(type: "int", nullable: false),
                    NegativeFeedbackCount = table.Column<int>(type: "int", nullable: false),
                    SuccessfulConversations = table.Column<int>(type: "int", nullable: false),
                    ToolPerformanceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopIssuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalConversations = table.Column<int>(type: "int", nullable: false),
                    TotalFeedbackCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversationAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAgentUsed = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LastMessageAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModelName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PromptVariantId = table.Column<int>(type: "int", nullable: true),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    State_AwaitingConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    State_LastShoppingIntentJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    State_LastSuccessfulToolResult = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    State_PendingAction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State_PendingIntentJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    State_PendingProductId = table.Column<int>(type: "int", nullable: true),
                    State_PendingProductName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    State_PendingQuantity = table.Column<int>(type: "int", nullable: false),
                    State_SelectedProductId = table.Column<int>(type: "int", nullable: true),
                    State_SelectedStoreName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiConversations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiImprovementSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SuggestedChange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
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
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExampleQuestions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstDetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastDetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiKnowledgeGaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiLongTermMemories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessCount = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Importance = table.Column<double>(type: "float", nullable: false),
                    LastAccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MemoryType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceConversationId = table.Column<int>(type: "int", nullable: false),
                    SourceConversationSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiLongTermMemories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiPromptVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AverageSatisfaction = table.Column<double>(type: "float", nullable: false),
                    ConversionRate = table.Column<double>(type: "float", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExperimentEndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExperimentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExperimentStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    SuccessfulOutcomes = table.Column<int>(type: "int", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalConversations = table.Column<int>(type: "int", nullable: false),
                    TotalOutcomes = table.Column<int>(type: "int", nullable: false),
                    TrafficPercentage = table.Column<double>(type: "float", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WinnerDecision = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPromptVariants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiShopContexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomInstructionsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreferredCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ReportingPreferencesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ShopPoliciesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ShopProfileId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
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
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                    PreferenceKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreferenceValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUserPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessAdvisorMemories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Importance = table.Column<int>(type: "int", nullable: false),
                    MemoryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAdvisorMemories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessAdvisorReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HealthScore = table.Column<int>(type: "int", nullable: true),
                    ReportContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAdvisorReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentProgress = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TargetMetric = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreTelegramNotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Daily"),
                    Hour = table.Column<int>(type: "int", nullable: false, defaultValue: 9),
                    IntervalDays = table.Column<int>(type: "int", nullable: true),
                    LastSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Minute = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NotificationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                    WeeklyDay = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreTelegramNotificationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreTelegramSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BotConnected = table.Column<bool>(type: "bit", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreTelegramSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiEpisodicMemories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EpisodeDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EpisodeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Importance = table.Column<double>(type: "float", nullable: false),
                    KeyEntitiesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SentimentAtEnd = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
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
                name: "AiMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiMessages", x => x.Id);
                    table.CheckConstraint("CK_AiMessages_Role", "[Role] IN ('user','assistant','system','tool')");
                    table.ForeignKey(
                        name: "FK_AiMessages_AiConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AiConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiMessages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoalId = table.Column<int>(type: "int", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VerificationCriteria = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VerificationResult = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessRecommendations_BusinessGoals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "BusinessGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AiFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    MessageId = table.Column<int>(type: "int", nullable: true),
                    AgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeedbackSource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FeedbackType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ImplicitSignal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
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
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AiToolCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    MessageId = table.Column<int>(type: "int", nullable: true),
                    ArgumentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ToolCallId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
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
                name: "IX_AiConversationAnalyses_AnalysisDate",
                table: "AiConversationAnalyses",
                column: "AnalysisDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_UserId",
                table: "AiConversations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversations_UserId_UpdatedAtUtc",
                table: "AiConversations",
                columns: new[] { "UserId", "UpdatedAtUtc" });

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
                name: "IX_AiMessages_ConversationId",
                table: "AiMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiMessages_ConversationId_SequenceNumber",
                table: "AiMessages",
                columns: new[] { "ConversationId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiMessages_ConversationId_UserId",
                table: "AiMessages",
                columns: new[] { "ConversationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiMessages_UserId",
                table: "AiMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptVariants_AgentName_IsActive",
                table: "AiPromptVariants",
                columns: new[] { "AgentName", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptVariants_AgentName_VariantName",
                table: "AiPromptVariants",
                columns: new[] { "AgentName", "VariantName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiPromptVariants_ExperimentName",
                table: "AiPromptVariants",
                column: "ExperimentName");

            migrationBuilder.CreateIndex(
                name: "IX_AiShopContexts_UserId",
                table: "AiShopContexts",
                column: "UserId",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_AiUserPreferences_UserId_PreferenceKey",
                table: "AiUserPreferences",
                columns: new[] { "UserId", "PreferenceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAdvisorMemories_ExpiresAtUtc",
                table: "BusinessAdvisorMemories",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAdvisorMemories_StoreUserId",
                table: "BusinessAdvisorMemories",
                column: "StoreUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAdvisorMemories_StoreUserId_MemoryType",
                table: "BusinessAdvisorMemories",
                columns: new[] { "StoreUserId", "MemoryType" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAdvisorReports_StoreUserId",
                table: "BusinessAdvisorReports",
                column: "StoreUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAdvisorReports_StoreUserId_ReportType_CreatedAtUtc",
                table: "BusinessAdvisorReports",
                columns: new[] { "StoreUserId", "ReportType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessGoals_StoreUserId",
                table: "BusinessGoals",
                column: "StoreUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessGoals_StoreUserId_Status",
                table: "BusinessGoals",
                columns: new[] { "StoreUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRecommendations_CreatedAtUtc",
                table: "BusinessRecommendations",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRecommendations_GoalId",
                table: "BusinessRecommendations",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRecommendations_StoreUserId",
                table: "BusinessRecommendations",
                column: "StoreUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRecommendations_StoreUserId_Status",
                table: "BusinessRecommendations",
                columns: new[] { "StoreUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreTelegramNotificationSettings_Enabled_NotificationType",
                table: "StoreTelegramNotificationSettings",
                columns: new[] { "Enabled", "NotificationType" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreTelegramNotificationSettings_StoreUserId",
                table: "StoreTelegramNotificationSettings",
                column: "StoreUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreTelegramNotificationSettings_StoreUserId_NotificationType",
                table: "StoreTelegramNotificationSettings",
                columns: new[] { "StoreUserId", "NotificationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreTelegramSettings_StoreUserId",
                table: "StoreTelegramSettings",
                column: "StoreUserId",
                unique: true);
        }
    }
}
