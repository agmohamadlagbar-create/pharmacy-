using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreTelegramNotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Daily"),
                    IntervalDays = table.Column<int>(type: "int", nullable: true),
                    WeeklyDay = table.Column<int>(type: "int", nullable: true),
                    Hour = table.Column<int>(type: "int", nullable: false, defaultValue: 9),
                    Minute = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                    LastSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    BotConnected = table.Column<bool>(type: "bit", nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreTelegramSettings", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreTelegramNotificationSettings");

            migrationBuilder.DropTable(
                name: "StoreTelegramSettings");
        }
    }
}
