using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreAnalyticsRestructuring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreAnalyticsDailies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    AverageOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductSalesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreAnalyticsDailies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreAnalyticsEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreAnalyticsEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreAnalyticsMonthlies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    AverageOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductSalesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreAnalyticsMonthlies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreAnalyticsYearlies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    AverageOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductSalesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreAnalyticsYearlies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreAnalyticsDailies_Date",
                table: "StoreAnalyticsDailies",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_StoreAnalyticsDailies_StoreUserId_Date",
                table: "StoreAnalyticsDailies",
                columns: new[] { "StoreUserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreAnalyticsEvents_CreatedAtUtc",
                table: "StoreAnalyticsEvents",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StoreAnalyticsEvents_StoreUserId",
                table: "StoreAnalyticsEvents",
                column: "StoreUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreAnalyticsEvents_StoreUserId_CreatedAtUtc",
                table: "StoreAnalyticsEvents",
                columns: new[] { "StoreUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreAnalyticsMonthlies_StoreUserId_Year_Month",
                table: "StoreAnalyticsMonthlies",
                columns: new[] { "StoreUserId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreAnalyticsYearlies_StoreUserId_Year",
                table: "StoreAnalyticsYearlies",
                columns: new[] { "StoreUserId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreAnalyticsDailies");

            migrationBuilder.DropTable(
                name: "StoreAnalyticsEvents");

            migrationBuilder.DropTable(
                name: "StoreAnalyticsMonthlies");

            migrationBuilder.DropTable(
                name: "StoreAnalyticsYearlies");
        }
    }
}
