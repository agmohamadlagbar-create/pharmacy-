using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;
using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

/// <summary>
/// Registers all tools into the ToolRegistry at application startup.
/// Bridges existing service layer (ProductSearch, Cart, CustomerSupport, etc.)
/// with the centralized tool registry used by agents.
/// </summary>
public static class ToolRegistrationService
{
    public static void RegisterAllTools(
        ToolRegistry registry,
        IServiceProvider services)
    {
        // Each tool registration needs a factory that creates a scoped executor.
        // Since tools are called within scoped service contexts (per-request),
        // we capture the IServiceProvider and resolve scoped services at execution time.

        // ═══════════════════════════════════════════════════
        //  Shopping Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "search_products",
            ToolDefinitions.SearchProducts,
            allowedAgents: new HashSet<string> { "shopping" },
            allowedRoles: new HashSet<string> { "Customer", "Shop", "Admin" },
            executor: async (argsJson, userId, conversationId) =>
            {
                using var scope = services.CreateScope();
                var productSearch = scope.ServiceProvider.GetRequiredService<IProductSearchService>();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var args = JsonSerializer.Deserialize<SearchProductsArgs>(argsJson);
                var productName = args?.ProductName ?? "";

                double userLat = 0, userLon = 0;
                var user = await db.Users.Cast<AppUser>().FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null) { userLat = user.Latitude ?? 0; userLon = user.Longitude ?? 0; }

                var searchResult = await productSearch.SearchProductsAsync(productName, userLat, userLon);
                if (args != null && args.Quantity > 0)
                {
                    searchResult.AllAvailable = searchResult.AllAvailable
                        .Where(p => p.Stock >= args.Quantity).ToList();
                }

                // Save pending product to conversation state
                var topProduct = searchResult.AllAvailable.FirstOrDefault();
                if (topProduct != null)
                {
                    var conversation = await db.AiConversations.FindAsync(conversationId);
                    if (conversation != null)
                    {
                        conversation.State.PendingProductId = topProduct.ProductId;
                        conversation.State.PendingProductName = topProduct.ProductName;
                        conversation.State.PendingQuantity = args?.Quantity > 0 ? args.Quantity : 1;
                        conversation.State.PendingAction = "add_to_cart";
                        conversation.State.AwaitingConfirmation = true;
                        await db.SaveChangesAsync();
                    }
                }

                // Save to the Conversation State Manager for Reference Resolving
                var productIds = searchResult.AllAvailable.Select(p => p.ProductId).ToList();
                var dbProducts = await db.Products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.Description })
                    .ToListAsync();

                var stateManager = scope.ServiceProvider.GetRequiredService<IConversationStateManager>();
                var presentedProducts = searchResult.AllAvailable.Select((p, idx) => {
                    var dbP = dbProducts.FirstOrDefault(dp => dp.Id == p.ProductId);
                    return new PresentedProductInfo
                    {
                        Index = idx + 1,
                        ProductId = p.ProductId,
                        Name = p.ProductName,
                        Price = p.Price,
                        Description = dbP?.Description
                    };
                }).ToList();

                stateManager.SavePresentedProducts(conversationId, presentedProducts);

                var sanitizedResult = new
                {
                    Count = searchResult.AllAvailable.Count,
                    Products = searchResult.AllAvailable.Select(p => new
                    {
                        p.ProductName, p.Price, p.Stock, p.StoreName, p.DistanceKm
                    })
                };
                return JsonSerializer.Serialize(sanitizedResult);
            });

        registry.RegisterTool(
            "add_to_cart",
            ToolDefinitions.AddToCart,
            allowedAgents: new HashSet<string> { "shopping" },
            allowedRoles: new HashSet<string> { "Customer" },
            executor: async (argsJson, userId, conversationId) =>
            {
                using var scope = services.CreateScope();
                var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var args = JsonSerializer.Deserialize<CartActionArgs>(argsJson);
                int productIdToUse = args?.ProductId ?? 0;

                if (productIdToUse <= 0)
                {
                    var conversation = await db.AiConversations.FindAsync(conversationId);
                    if (conversation?.State.PendingProductId != null)
                        productIdToUse = conversation.State.PendingProductId.Value;
                }

                if (productIdToUse <= 0)
                    return "Error: Could not identify which product to add. Please search for the product first.";

                var qty = args?.Quantity > 0 ? args.Quantity : 1;
                var result = await cartService.AddToCartAsync(userId, productIdToUse, qty);

                if (!result.StartsWith("Error"))
                {
                    try
                    {
                        var abService = scope.ServiceProvider.GetRequiredService<IAbTestingService>();
                        await abService.RecordOutcomeAsync(conversationId, true);
                    }
                    catch (Exception)
                    {
                        // Log locally or ignore to prevent crashing the tool
                    }
                }

                // Clear conversation state
                var conv = await db.AiConversations.FindAsync(conversationId);
                if (conv != null) { conv.State = new(); await db.SaveChangesAsync(); }

                return result;
            });

        registry.RegisterTool(
            "update_quantity",
            ToolDefinitions.UpdateQuantity,
            allowedAgents: new HashSet<string> { "shopping" },
            allowedRoles: new HashSet<string> { "Customer" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                var args = JsonSerializer.Deserialize<CartActionArgs>(argsJson);
                return args != null
                    ? await cartService.UpdateQuantityAsync(userId, args.ProductId, args.Quantity)
                    : "Error: invalid arguments.";
            });

        registry.RegisterTool(
            "remove_from_cart",
            ToolDefinitions.RemoveFromCart,
            allowedAgents: new HashSet<string> { "shopping" },
            allowedRoles: new HashSet<string> { "Customer" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                var args = JsonSerializer.Deserialize<CartActionArgs>(argsJson);
                return args != null
                    ? await cartService.RemoveFromCartAsync(userId, args.ProductId)
                    : "Error: invalid arguments.";
            });

        registry.RegisterTool(
            "get_cart",
            ToolDefinitions.GetCart,
            allowedAgents: new HashSet<string> { "shopping" },
            allowedRoles: new HashSet<string> { "Customer" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                return await cartService.GetCartSummaryAsync(userId);
            });

        registry.RegisterTool(
            "find_best_store",
            ToolDefinitions.FindBestStore,
            allowedAgents: new HashSet<string> { "shopping" },
            allowedRoles: new HashSet<string> { "Customer" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var storeSelection = scope.ServiceProvider.GetRequiredService<IStoreSelectionService>();
                var args = JsonSerializer.Deserialize<FindBestStoreArgs>(argsJson);
                return args != null
                    ? JsonSerializer.Serialize(await storeSelection.FindBestStoreAsync(
                        args.ProductName, args.Quantity, args.CustomerLatitude, args.CustomerLongitude))
                    : "Error: invalid arguments.";
            });

        // ═══════════════════════════════════════════════════
        //  Customer Support Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_recent_orders",
            ToolDefinitions.GetRecentOrders,
            allowedAgents: new HashSet<string> { "customer_service", "order_tracking" },
            allowedRoles: new HashSet<string> { "Customer", "User" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var orders = await db.Orders
                    .AsNoTracking()
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAtUtc)
                    .Take(5)
                    .Select(o => new {
                        OrderId = o.Id,
                        Status = o.Status.ToString(),
                        o.TotalAmount,
                        o.CreatedAtUtc
                    })
                    .ToListAsync();
                return JsonSerializer.Serialize(orders);
            });

        registry.RegisterTool(
            "track_order",
            ToolDefinitions.TrackOrder,
            allowedAgents: new HashSet<string> { "customer_service", "order_tracking" },
            allowedRoles: new HashSet<string> { "Customer", "User" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var support = scope.ServiceProvider.GetRequiredService<ICustomerSupportService>();
                var args = JsonSerializer.Deserialize<OrderActionArgs>(argsJson);
                return args != null
                    ? JsonSerializer.Serialize(await support.TrackOrderAsync(userId, args.OrderId))
                    : "Error: invalid arguments.";
            });

        registry.RegisterTool(
            "get_delivery_eta",
            ToolDefinitions.GetDeliveryEta,
            allowedAgents: new HashSet<string> { "customer_service", "order_tracking" },
            allowedRoles: new HashSet<string> { "Customer", "User" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var support = scope.ServiceProvider.GetRequiredService<ICustomerSupportService>();
                var args = JsonSerializer.Deserialize<OrderActionArgs>(argsJson);
                return args != null
                    ? JsonSerializer.Serialize(await support.GetDeliveryEtaAsync(userId, args.OrderId))
                    : "Error: invalid arguments.";
            });

        registry.RegisterTool(
            "get_refund_status",
            ToolDefinitions.GetRefundStatus,
            allowedAgents: new HashSet<string> { "customer_service" },
            allowedRoles: new HashSet<string> { "Customer", "User" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var support = scope.ServiceProvider.GetRequiredService<ICustomerSupportService>();
                var args = JsonSerializer.Deserialize<OrderActionArgs>(argsJson);
                return args != null
                    ? JsonSerializer.Serialize(await support.GetRefundStatusAsync(userId, args.OrderId))
                    : "Error: invalid arguments.";
            });

        registry.RegisterTool(
            "get_order_details",
            ToolDefinitions.GetOrderDetails,
            allowedAgents: new HashSet<string> { "customer_service" },
            allowedRoles: new HashSet<string> { "Customer", "User" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var support = scope.ServiceProvider.GetRequiredService<ICustomerSupportService>();
                var args = JsonSerializer.Deserialize<OrderActionArgs>(argsJson);
                return args != null
                    ? JsonSerializer.Serialize(await support.GetOrderDetailsAsync(userId, args.OrderId))
                    : "Error: invalid arguments.";
            });

        // ═══════════════════════════════════════════════════
        //  Recommendation Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_recommendations",
            ToolDefinitions.GetRecommendations,
            allowedAgents: new HashSet<string> { "recommendation", "shopping" },
            allowedRoles: new HashSet<string> { "Customer" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var recService = scope.ServiceProvider.GetRequiredService<IRecommendationService>();
                var args = JsonSerializer.Deserialize<RecommendationArgs>(argsJson);
                int? pid = args?.ProductId > 0 ? args.ProductId : null;
                var recommendations = await recService.GetRecommendationsAsync(userId, pid);
                return JsonSerializer.Serialize(recommendations);
            });

        // ═══════════════════════════════════════════════════
        //  Shop Owner Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "generate_product_description",
            ToolDefinitions.GenerateProductDescription,
            allowedAgents: new HashSet<string> { "product_management" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var pmService = scope.ServiceProvider.GetRequiredService<IProductManagementAiService>();
                var args = JsonSerializer.Deserialize<ProductDescArgs>(argsJson);
                return await pmService.GenerateDescriptionAsync(
                    args?.ProductName ?? "", args?.Category ?? "", args?.ExistingDescription);
            });

        registry.RegisterTool(
            "generate_seo_keywords",
            ToolDefinitions.GenerateSeoKeywords,
            allowedAgents: new HashSet<string> { "product_management" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var pmService = scope.ServiceProvider.GetRequiredService<IProductManagementAiService>();
                var args = JsonSerializer.Deserialize<SeoKeywordsArgs>(argsJson);
                return await pmService.GenerateSeoKeywordsAsync(
                    args?.ProductName ?? "", args?.Description ?? "");
            });

        registry.RegisterTool(
            "generate_report",
            ToolDefinitions.GenerateReport,
            allowedAgents: new HashSet<string> { "reporting" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
                var args = JsonSerializer.Deserialize<ReportArgs>(argsJson);

                DateTime? startDate = null, endDate = null;
                if (!string.IsNullOrWhiteSpace(args?.StartDate))
                    DateTime.TryParse(args.StartDate, out var sd).ToString(); // Parse attempt
                if (!string.IsNullOrWhiteSpace(args?.EndDate))
                    DateTime.TryParse(args.EndDate, out var ed).ToString();

                DateTime.TryParse(args?.StartDate, out var parsedStart);
                DateTime.TryParse(args?.EndDate, out var parsedEnd);
                startDate = parsedStart == default ? null : parsedStart;
                endDate = parsedEnd == default ? null : parsedEnd;

                var report = await analytics.GetFullAnalyticsAsync(userId, startDate, endDate);
                return JsonSerializer.Serialize(report);
            });

        registry.RegisterTool(
            "analyze_sales_trend",
            ToolDefinitions.AnalyzeSalesTrend,
            allowedAgents: new HashSet<string> { "analytics" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
                var args = JsonSerializer.Deserialize<SalesTrendArgs>(argsJson);
                var period = args?.Period?.ToLowerInvariant() ?? "month";

                var now = DateTime.UtcNow;
                DateTime startDate = period switch
                {
                    "week" => now.AddDays(-7),
                    "quarter" => now.AddMonths(-3),
                    "year" => now.AddYears(-1),
                    _ => now.AddMonths(-1) // month
                };

                var report = await analytics.GetFullAnalyticsAsync(userId, startDate, now);
                return JsonSerializer.Serialize(report);
            });

        registry.RegisterTool(
            "check_low_stock",
            ToolDefinitions.CheckLowStock,
            allowedAgents: new HashSet<string> { "inventory" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryIntelligenceService>();
                var result = await inventoryService.GetLowStockProductsAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_restock_suggestions",
            ToolDefinitions.GetRestockSuggestions,
            allowedAgents: new HashSet<string> { "inventory" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryIntelligenceService>();
                var result = await inventoryService.GetRestockSuggestionsAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_fast_selling_products",
            ToolDefinitions.GetFastSellingProducts,
            allowedAgents: new HashSet<string> { "inventory" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryIntelligenceService>();
                var args = JsonSerializer.Deserialize<FastSellingArgs>(argsJson);
                var result = await inventoryService.GetFastSellingProductsAsync(userId, args?.Days ?? 7);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_inventory_health_score",
            ToolDefinitions.GetInventoryHealthScore,
            allowedAgents: new HashSet<string> { "inventory" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryIntelligenceService>();
                var result = await inventoryService.GetInventoryHealthScoreAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_seasonal_demand_forecast",
            ToolDefinitions.GetSeasonalDemandForecast,
            allowedAgents: new HashSet<string> { "inventory" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryIntelligenceService>();
                var result = await inventoryService.GetSeasonalDemandForecastAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        // ═══════════════════════════════════════════════════
        //  Marketing Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_marketing_insights",
            ToolDefinitions.GetMarketingInsights,
            allowedAgents: new HashSet<string> { "marketing" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var marketingService = scope.ServiceProvider.GetRequiredService<IMarketingService>();
                var result = await marketingService.GetMarketingInsightsAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "suggest_promotions",
            ToolDefinitions.SuggestPromotions,
            allowedAgents: new HashSet<string> { "marketing" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var marketingService = scope.ServiceProvider.GetRequiredService<IMarketingService>();
                var result = await marketingService.SuggestPromotionsAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "suggest_bundles",
            ToolDefinitions.SuggestBundles,
            allowedAgents: new HashSet<string> { "marketing" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var marketingService = scope.ServiceProvider.GetRequiredService<IMarketingService>();
                var result = await marketingService.SuggestBundlesAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        // ═══════════════════════════════════════════════════
        //  Customer Insight Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_customer_behavior_analysis",
            ToolDefinitions.GetCustomerBehaviorAnalysis,
            allowedAgents: new HashSet<string> { "customer_insights" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var insightService = scope.ServiceProvider.GetRequiredService<ICustomerInsightService>();
                var result = await insightService.GetCustomerBehaviorAnalysisAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_customer_satisfaction_stats",
            ToolDefinitions.GetCustomerSatisfactionStats,
            allowedAgents: new HashSet<string> { "customer_insights" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var insightService = scope.ServiceProvider.GetRequiredService<ICustomerInsightService>();
                var result = await insightService.GetCustomerSatisfactionStatsAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_top_customers",
            ToolDefinitions.GetTopCustomers,
            allowedAgents: new HashSet<string> { "customer_insights" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var insightService = scope.ServiceProvider.GetRequiredService<ICustomerInsightService>();
                var result = await insightService.GetTopCustomersAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        // ═══════════════════════════════════════════════════
        //  Admin: Fraud Detection Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "detect_suspicious_orders",
            ToolDefinitions.DetectSuspiciousOrders,
            allowedAgents: new HashSet<string> { "fraud_detection" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var fraudService = scope.ServiceProvider.GetRequiredService<IFraudDetectionService>();
                var args = JsonSerializer.Deserialize<FraudDaysArgs>(argsJson);
                var result = await fraudService.DetectSuspiciousOrdersAsync(args?.Days ?? 7);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "detect_duplicate_accounts",
            ToolDefinitions.DetectDuplicateAccounts,
            allowedAgents: new HashSet<string> { "fraud_detection" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (_, _, _2) =>
            {
                using var scope = services.CreateScope();
                var fraudService = scope.ServiceProvider.GetRequiredService<IFraudDetectionService>();
                var result = await fraudService.DetectDuplicateAccountsAsync();
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_fraud_summary",
            ToolDefinitions.GetFraudSummary,
            allowedAgents: new HashSet<string> { "fraud_detection" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var fraudService = scope.ServiceProvider.GetRequiredService<IFraudDetectionService>();
                var args = JsonSerializer.Deserialize<FraudSummaryArgs>(argsJson);
                DateTime? startDate = null, endDate = null;
                DateTime.TryParse(args?.StartDate, out var parsedStart);
                DateTime.TryParse(args?.EndDate, out var parsedEnd);
                startDate = parsedStart == default ? null : parsedStart;
                endDate = parsedEnd == default ? null : parsedEnd;
                var result = await fraudService.GetFraudSummaryAsync(startDate, endDate);
                return JsonSerializer.Serialize(result);
            });

        // ═══════════════════════════════════════════════════
        //  Admin: Security Monitoring Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_security_events",
            ToolDefinitions.GetSecurityEvents,
            allowedAgents: new HashSet<string> { "security_monitoring" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var securityService = scope.ServiceProvider.GetRequiredService<ISecurityMonitoringService>();
                var args = JsonSerializer.Deserialize<SecurityEventsArgs>(argsJson);
                var result = await securityService.GetSecurityEventsAsync(args?.Days ?? 7, args?.Severity);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_login_anomalies",
            ToolDefinitions.GetLoginAnomalies,
            allowedAgents: new HashSet<string> { "security_monitoring" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var securityService = scope.ServiceProvider.GetRequiredService<ISecurityMonitoringService>();
                var args = JsonSerializer.Deserialize<SecurityDaysArgs>(argsJson);
                var result = await securityService.GetLoginAnomaliesAsync(args?.Days ?? 7);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_rate_limit_violations",
            ToolDefinitions.GetRateLimitViolations,
            allowedAgents: new HashSet<string> { "security_monitoring" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var securityService = scope.ServiceProvider.GetRequiredService<ISecurityMonitoringService>();
                var args = JsonSerializer.Deserialize<SecurityDaysArgs>(argsJson);
                var result = await securityService.GetRateLimitViolationsAsync(args?.Days ?? 7);
                return JsonSerializer.Serialize(result);
            });

        // ═══════════════════════════════════════════════════
        //  Admin: Platform Analytics Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_platform_overview",
            ToolDefinitions.GetPlatformOverview,
            allowedAgents: new HashSet<string> { "platform_analytics" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (_, _, _2) =>
            {
                using var scope = services.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<IPlatformAnalyticsService>();
                var result = await analyticsService.GetPlatformOverviewAsync();
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_store_rankings",
            ToolDefinitions.GetStoreRankings,
            allowedAgents: new HashSet<string> { "platform_analytics" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<IPlatformAnalyticsService>();
                var args = JsonSerializer.Deserialize<StoreRankingsArgs>(argsJson);
                var result = await analyticsService.GetStoreRankingsAsync(args?.Metric ?? "revenue", args?.Top ?? 10);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_category_trends",
            ToolDefinitions.GetCategoryTrends,
            allowedAgents: new HashSet<string> { "platform_analytics" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<IPlatformAnalyticsService>();
                var args = JsonSerializer.Deserialize<CategoryTrendsArgs>(argsJson);
                var result = await analyticsService.GetCategoryTrendsAsync(args?.Days ?? 30);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_regional_analytics",
            ToolDefinitions.GetRegionalAnalytics,
            allowedAgents: new HashSet<string> { "platform_analytics" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (_, _, _2) =>
            {
                using var scope = services.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<IPlatformAnalyticsService>();
                var result = await analyticsService.GetRegionalAnalyticsAsync();
                return JsonSerializer.Serialize(result);
            });

        // ═══════════════════════════════════════════════════
        //  Admin: Store Approval Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_pending_stores",
            ToolDefinitions.GetPendingStores,
            allowedAgents: new HashSet<string> { "store_approval" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (_, _, _2) =>
            {
                using var scope = services.CreateScope();
                var approvalService = scope.ServiceProvider.GetRequiredService<IStoreApprovalService>();
                var result = await approvalService.GetPendingStoresAsync();
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_store_risk_score",
            ToolDefinitions.GetStoreRiskScore,
            allowedAgents: new HashSet<string> { "store_approval" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var approvalService = scope.ServiceProvider.GetRequiredService<IStoreApprovalService>();
                var args = JsonSerializer.Deserialize<StoreUserIdArgs>(argsJson);
                var result = await approvalService.GetStoreRiskScoreAsync(args?.StoreUserId ?? "");
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_store_compliance_check",
            ToolDefinitions.GetStoreComplianceCheck,
            allowedAgents: new HashSet<string> { "store_approval" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var approvalService = scope.ServiceProvider.GetRequiredService<IStoreApprovalService>();
                var args = JsonSerializer.Deserialize<StoreUserIdArgs>(argsJson);
                var result = await approvalService.GetStoreComplianceCheckAsync(args?.StoreUserId ?? "");
                return JsonSerializer.Serialize(result);
            });

        // ═══════════════════════════════════════════════════
        //  Admin: Moderation Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_flagged_content",
            ToolDefinitions.GetFlaggedContent,
            allowedAgents: new HashSet<string> { "moderation" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var moderationService = scope.ServiceProvider.GetRequiredService<IModerationService>();
                var args = JsonSerializer.Deserialize<FlaggedContentArgs>(argsJson);
                var result = await moderationService.GetFlaggedContentAsync(args?.ContentType);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_review_moderation",
            ToolDefinitions.GetReviewModeration,
            allowedAgents: new HashSet<string> { "moderation" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (_, _, _2) =>
            {
                using var scope = services.CreateScope();
                var moderationService = scope.ServiceProvider.GetRequiredService<IModerationService>();
                var result = await moderationService.GetReviewModerationAsync();
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_pricing_anomalies",
            ToolDefinitions.GetPricingAnomalies,
            allowedAgents: new HashSet<string> { "moderation" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (_, _, _2) =>
            {
                using var scope = services.CreateScope();
                var moderationService = scope.ServiceProvider.GetRequiredService<IModerationService>();
                var result = await moderationService.GetPricingAnomaliesAsync();
                return JsonSerializer.Serialize(result);
            });

        // ═══════════════════════════════════════════════════
        //  Admin: Global Support Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_escalated_issues",
            ToolDefinitions.GetEscalatedIssues,
            allowedAgents: new HashSet<string> { "global_support" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (_, _, _2) =>
            {
                using var scope = services.CreateScope();
                var supportService = scope.ServiceProvider.GetRequiredService<IGlobalSupportService>();
                var result = await supportService.GetEscalatedIssuesAsync();
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_cross_store_disputes",
            ToolDefinitions.GetCrossStoreDisputes,
            allowedAgents: new HashSet<string> { "global_support" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (_, _, _2) =>
            {
                using var scope = services.CreateScope();
                var supportService = scope.ServiceProvider.GetRequiredService<IGlobalSupportService>();
                var result = await supportService.GetCrossStoreDisputesAsync();
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_user_history",
            ToolDefinitions.GetUserHistory,
            allowedAgents: new HashSet<string> { "global_support" },
            allowedRoles: new HashSet<string> { "Admin" },
            executor: async (argsJson, _, _2) =>
            {
                using var scope = services.CreateScope();
                var supportService = scope.ServiceProvider.GetRequiredService<IGlobalSupportService>();
                var args = JsonSerializer.Deserialize<UserHistoryArgs>(argsJson);
                var result = await supportService.GetUserHistoryAsync(args?.UserId ?? "");
                return JsonSerializer.Serialize(result);
            });

        // ═══════════════════════════════════════════════════
        //  Business Advisor Tools
        // ═══════════════════════════════════════════════════

        registry.RegisterTool(
            "get_store_inspection",
            ToolDefinitions.GetStoreInspection,
            allowedAgents: new HashSet<string> { "business_advisor" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
                var result = await advisorService.InspectStoreAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "get_business_goals",
            ToolDefinitions.GetBusinessGoals,
            allowedAgents: new HashSet<string> { "business_advisor" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
                var result = await advisorService.GetGoalsAsync(userId);
                return JsonSerializer.Serialize(result.Select(g => new
                {
                    g.Id, g.Title, g.Description, g.TargetMetric, g.TargetValue,
                    g.CurrentProgress, Status = g.Status.ToString(), g.CreatedAtUtc
                }));
            });

        registry.RegisterTool(
            "get_recommendation_history",
            ToolDefinitions.GetRecommendationHistory,
            allowedAgents: new HashSet<string> { "business_advisor" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
                var args = JsonSerializer.Deserialize<RecommendationHistoryArgs>(argsJson);
                var result = await advisorService.GetRecommendationHistoryAsync(userId, args?.Count ?? 20);
                return JsonSerializer.Serialize(result.Select(r => new
                {
                    r.Id, r.GoalId, r.Title, r.Description, r.Reason,
                    Priority = r.Priority.ToString(), Status = r.Status.ToString(),
                    r.VerificationCriteria, r.VerificationResult,
                    r.CreatedAtUtc, r.DueDate, r.CompletedAtUtc
                }));
            });

        registry.RegisterTool(
            "get_business_health_score",
            ToolDefinitions.GetBusinessHealthScore,
            allowedAgents: new HashSet<string> { "business_advisor" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (_, userId, _2) =>
            {
                using var scope = services.CreateScope();
                var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
                var result = await advisorService.CalculateHealthScoreAsync(userId);
                return JsonSerializer.Serialize(result);
            });

        registry.RegisterTool(
            "create_recommendation",
            ToolDefinitions.CreateRecommendation,
            allowedAgents: new HashSet<string> { "business_advisor" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
                var args = JsonSerializer.Deserialize<CreateRecommendationArgs>(argsJson);
                if (args == null || string.IsNullOrWhiteSpace(args.Title))
                    return "Error: title is required.";

                var priority = Enum.TryParse<OnlineStore.Domain.Entities.RecommendationPriority>(args.Priority, true, out var p)
                    ? p : OnlineStore.Domain.Entities.RecommendationPriority.Medium;

                var rec = await advisorService.CreateRecommendationAsync(
                    userId, args.GoalId, args.Title, args.Description,
                    args.Reason, priority, args.VerificationCriteria, null);

                return JsonSerializer.Serialize(new { rec.Id, rec.Title, Status = rec.Status.ToString(), Message = "Recommendation created successfully." });
            });

        registry.RegisterTool(
            "update_recommendation_status",
            ToolDefinitions.UpdateRecommendationStatus,
            allowedAgents: new HashSet<string> { "business_advisor" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
                var args = JsonSerializer.Deserialize<UpdateRecommendationStatusArgs>(argsJson);
                if (args == null || args.RecommendationId <= 0)
                    return "Error: recommendationId is required.";

                var status = Enum.TryParse<OnlineStore.Domain.Entities.RecommendationStatus>(args.Status, true, out var s)
                    ? s : OnlineStore.Domain.Entities.RecommendationStatus.Completed;

                await advisorService.UpdateRecommendationStatusAsync(args.RecommendationId, status, args.VerificationResult);
                return JsonSerializer.Serialize(new { args.RecommendationId, Status = status.ToString(), Message = "Recommendation status updated." });
            });

        registry.RegisterTool(
            "get_advisor_memories",
            ToolDefinitions.GetAdvisorMemories,
            allowedAgents: new HashSet<string> { "business_advisor" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
                var args = JsonSerializer.Deserialize<AdvisorMemoriesArgs>(argsJson);
                var result = await advisorService.GetMemoriesAsync(userId, args?.Count ?? 30);
                return JsonSerializer.Serialize(result.Select(m => new
                {
                    m.Id, m.MemoryType, m.Content, m.Importance, m.CreatedAtUtc
                }));
            });

        registry.RegisterTool(
            "save_advisor_memory",
            ToolDefinitions.SaveAdvisorMemory,
            allowedAgents: new HashSet<string> { "business_advisor" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
                var args = JsonSerializer.Deserialize<SaveAdvisorMemoryArgs>(argsJson);
                if (args == null || string.IsNullOrWhiteSpace(args.Content))
                    return "Error: content is required.";

                await advisorService.SaveMemoryAsync(userId, args.MemoryType, args.Content, args.Importance);
                return JsonSerializer.Serialize(new { Message = "Memory saved successfully." });
            });

        registry.RegisterTool(
            "create_business_goal",
            ToolDefinitions.CreateBusinessGoal,
            allowedAgents: new HashSet<string> { "business_advisor" },
            allowedRoles: new HashSet<string> { "Shop" },
            executor: async (argsJson, userId, _) =>
            {
                using var scope = services.CreateScope();
                var advisorService = scope.ServiceProvider.GetRequiredService<IBusinessAdvisorService>();
                var args = JsonSerializer.Deserialize<CreateBusinessGoalArgs>(argsJson);
                if (args == null || string.IsNullOrWhiteSpace(args.Title))
                    return "Error: title is required.";

                var goal = await advisorService.CreateGoalAsync(
                    userId, args.Title, args.Description, args.TargetMetric, args.TargetValue);

                return JsonSerializer.Serialize(new { goal.Id, goal.Title, Status = goal.Status.ToString(), Message = "Business goal created successfully." });
            });

    }

    // ═══════════════════════════════════════════════════
    //  Argument DTOs for tool deserialization
    // ═══════════════════════════════════════════════════

    private class SearchProductsArgs
    {
        [JsonPropertyName("productName")] public string ProductName { get; set; } = "";
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
    }

    private class CartActionArgs
    {
        [JsonPropertyName("productId")] public int ProductId { get; set; }
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
    }

    private class OrderActionArgs
    {
        [JsonPropertyName("orderId")] public int OrderId { get; set; }
    }

    private class FindBestStoreArgs
    {
        [JsonPropertyName("productName")] public string ProductName { get; set; } = "";
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
        [JsonPropertyName("customerLatitude")] public double CustomerLatitude { get; set; }
        [JsonPropertyName("customerLongitude")] public double CustomerLongitude { get; set; }
    }

    private class RecommendationArgs
    {
        [JsonPropertyName("productId")] public int ProductId { get; set; }
    }

    private class ProductDescArgs
    {
        [JsonPropertyName("productName")] public string ProductName { get; set; } = "";
        [JsonPropertyName("category")] public string Category { get; set; } = "";
        [JsonPropertyName("existingDescription")] public string? ExistingDescription { get; set; }
    }

    private class SeoKeywordsArgs
    {
        [JsonPropertyName("productName")] public string ProductName { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
    }

    private class ReportArgs
    {
        [JsonPropertyName("query")] public string Query { get; set; } = "";
        [JsonPropertyName("startDate")] public string? StartDate { get; set; }
        [JsonPropertyName("endDate")] public string? EndDate { get; set; }
    }

    private class SalesTrendArgs
    {
        [JsonPropertyName("period")] public string Period { get; set; } = "month";
    }

    private class FastSellingArgs
    {
        [JsonPropertyName("days")] public int Days { get; set; } = 7;
    }

    private class FraudDaysArgs
    {
        [JsonPropertyName("days")] public int Days { get; set; } = 7;
    }

    private class FraudSummaryArgs
    {
        [JsonPropertyName("startDate")] public string? StartDate { get; set; }
        [JsonPropertyName("endDate")] public string? EndDate { get; set; }
    }

    private class SecurityEventsArgs
    {
        [JsonPropertyName("days")] public int Days { get; set; } = 7;
        [JsonPropertyName("severity")] public string? Severity { get; set; }
    }

    private class SecurityDaysArgs
    {
        [JsonPropertyName("days")] public int Days { get; set; } = 7;
    }

    private class StoreRankingsArgs
    {
        [JsonPropertyName("metric")] public string Metric { get; set; } = "revenue";
        [JsonPropertyName("top")] public int Top { get; set; } = 10;
    }

    private class CategoryTrendsArgs
    {
        [JsonPropertyName("days")] public int Days { get; set; } = 30;
    }

    private class StoreUserIdArgs
    {
        [JsonPropertyName("storeUserId")] public string StoreUserId { get; set; } = "";
    }

    private class FlaggedContentArgs
    {
        [JsonPropertyName("contentType")] public string? ContentType { get; set; }
    }

    private class UserHistoryArgs
    {
        [JsonPropertyName("userId")] public string UserId { get; set; } = "";
    }

    // ═══════════════════════════════════════════════════
    //  Business Advisor Tool Argument DTOs
    // ═══════════════════════════════════════════════════

    private class RecommendationHistoryArgs
    {
        [JsonPropertyName("count")] public int Count { get; set; } = 20;
    }

    private class CreateRecommendationArgs
    {
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("reason")] public string Reason { get; set; } = "";
        [JsonPropertyName("priority")] public string Priority { get; set; } = "Medium";
        [JsonPropertyName("verificationCriteria")] public string? VerificationCriteria { get; set; }
        [JsonPropertyName("goalId")] public int? GoalId { get; set; }
    }

    private class UpdateRecommendationStatusArgs
    {
        [JsonPropertyName("recommendationId")] public int RecommendationId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = "";
        [JsonPropertyName("verificationResult")] public string? VerificationResult { get; set; }
    }

    private class AdvisorMemoriesArgs
    {
        [JsonPropertyName("count")] public int Count { get; set; } = 30;
    }

    private class SaveAdvisorMemoryArgs
    {
        [JsonPropertyName("memoryType")] public string MemoryType { get; set; } = "Observation";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
        [JsonPropertyName("importance")] public int Importance { get; set; } = 5;
    }

    private class CreateBusinessGoalArgs
    {
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("targetMetric")] public string? TargetMetric { get; set; }
        [JsonPropertyName("targetValue")] public decimal? TargetValue { get; set; }
    }
}

