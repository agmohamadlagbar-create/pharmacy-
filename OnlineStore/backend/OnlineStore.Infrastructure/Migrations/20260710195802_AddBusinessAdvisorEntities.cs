using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAdvisorEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessAdvisorMemories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MemoryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Importance = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReportContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthScore = table.Column<int>(type: "int", nullable: true),
                    ReportDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetMetric = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentProgress = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GoalId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VerificationCriteria = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    VerificationResult = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessAdvisorMemories");

            migrationBuilder.DropTable(
                name: "BusinessAdvisorReports");

            migrationBuilder.DropTable(
                name: "BusinessRecommendations");

            migrationBuilder.DropTable(
                name: "BusinessGoals");
        }
    }
}
