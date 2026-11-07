using OnlineStore.Api.Contracts;

namespace OnlineStore.Api.Services.Agents;

/// <summary>
/// Static tool schema definitions extracted from GroqAiService.GetAvailableTools().
/// Organized by domain so agents can select only the tools they need.
/// </summary>
public static class ToolDefinitions
{
    // ═══════════════════════════════════════════════════
    //  Shopping Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto SearchProducts => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "search_products",
            Description = "Search for products by name globally. Returns stock availability, best prices and distance from user.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    productName = new { type = "string", description = "The simple name of the product only (e.g. 'apple', 'bread'). DO NOT include quantities or full phrases." },
                    quantity = new { type = "integer", description = "The required quantity." }
                },
                required = new[] { "productName", "quantity" }
            }
        }
    };

    public static GroqToolDto AddToCart => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "add_to_cart",
            Description = "Add a product to the authenticated user's cart. " +
                "CRITICAL: ONLY call this tool when the user has EXPLICITLY said 'add to cart', 'buy this', 'add this item', or an equivalent phrase. " +
                "NEVER call this after a search alone. Searching for a product is NOT a request to add it. " +
                "You MUST confirm with the user which product to add if multiple were found. " +
                "Only confirm success if the backend returns success==true or status=='success'.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    productId = new { type = "integer", description = "The ID of the product." },
                    quantity = new { type = "integer", description = "The quantity to add." }
                },
                required = new[] { "productId", "quantity" }
            }
        }
    };

    public static GroqToolDto UpdateQuantity => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "update_quantity",
            Description = "Update the quantity of a product already in the user's cart.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    productId = new { type = "integer", description = "The ID of the product." },
                    quantity = new { type = "integer", description = "The new quantity." }
                },
                required = new[] { "productId", "quantity" }
            }
        }
    };

    public static GroqToolDto RemoveFromCart => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "remove_from_cart",
            Description = "Remove a product from the user's cart.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    productId = new { type = "integer", description = "The ID of the product." }
                },
                required = new[] { "productId" }
            }
        }
    };

    public static GroqToolDto GetCart => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_cart",
            Description = "Get the current contents of the user's shopping cart, including products, quantities, and total price.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto FindBestStore => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "find_best_store",
            Description = "Find the best store for a product based on customer location, comparing distance, price, and stock availability. Returns the best overall, cheapest, and nearest options.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    productName = new { type = "string", description = "The name of the product to find." },
                    quantity = new { type = "integer", description = "Required quantity." },
                    customerLatitude = new { type = "number", description = "Customer's latitude coordinate." },
                    customerLongitude = new { type = "number", description = "Customer's longitude coordinate." }
                },
                required = new[] { "productName", "quantity", "customerLatitude", "customerLongitude" }
            }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Customer Support Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetRecentOrders => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_recent_orders",
            Description = "Retrieve a list of the authenticated user's recent orders to find their order IDs and status.",
            Parameters = new
            {
                type = "object",
                properties = new { }
            }
        }
    };

    public static GroqToolDto TrackOrder => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "track_order",
            Description = "Track the status of an order for the authenticated user.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    orderId = new { type = "integer", description = "The ID of the order." }
                },
                required = new[] { "orderId" }
            }
        }
    };

    public static GroqToolDto GetDeliveryEta => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_delivery_eta",
            Description = "Get the estimated delivery time for an order.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    orderId = new { type = "integer", description = "The ID of the order." }
                },
                required = new[] { "orderId" }
            }
        }
    };

    public static GroqToolDto GetRefundStatus => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_refund_status",
            Description = "Check the refund status of a cancelled order.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    orderId = new { type = "integer", description = "The ID of the order." }
                },
                required = new[] { "orderId" }
            }
        }
    };

    public static GroqToolDto GetOrderDetails => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_order_details",
            Description = "Get the full details of an order including its items.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    orderId = new { type = "integer", description = "The ID of the order." }
                },
                required = new[] { "orderId" }
            }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Recommendation Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetRecommendations => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_recommendations",
            Description = "Get personalized product recommendations for the user. Includes frequently bought together, related by category, smart upsells, and purchase-history-based suggestions. Optionally scoped to a specific product.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    productId = new { type = "integer", description = "Optional product ID to get recommendations relative to a specific product. Pass 0 or omit for general recommendations." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Shop Owner Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GenerateProductDescription => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "generate_product_description",
            Description = "Generate an AI-written product description for a given product name and category.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    productName = new { type = "string", description = "The name of the product." },
                    category = new { type = "string", description = "The product category." },
                    existingDescription = new { type = "string", description = "Optional existing description to improve." }
                },
                required = new[] { "productName" }
            }
        }
    };

    public static GroqToolDto GenerateSeoKeywords => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "generate_seo_keywords",
            Description = "Generate SEO keywords for a product to improve search visibility.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    productName = new { type = "string", description = "The name of the product." },
                    description = new { type = "string", description = "The product description." }
                },
                required = new[] { "productName" }
            }
        }
    };

    public static GroqToolDto GenerateReport => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "generate_report",
            Description = "Generate a business report based on a natural language query. Examples: 'monthly revenue', 'top selling products', 'revenue comparison'.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "The natural language report request." },
                    startDate = new { type = "string", description = "Optional start date (ISO format)." },
                    endDate = new { type = "string", description = "Optional end date (ISO format)." }
                },
                required = new[] { "query" }
            }
        }
    };

    public static GroqToolDto AnalyzeSalesTrend => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "analyze_sales_trend",
            Description = "Analyze sales trends and provide insights about business performance. Explains why sales are increasing or decreasing.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    period = new { type = "string", description = "The time period to analyze: 'week', 'month', 'quarter', 'year'." }
                },
                required = new[] { "period" }
            }
        }
    };

    public static GroqToolDto CheckLowStock => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "check_low_stock",
            Description = "Check which products are running low on stock and may need restocking.",
            Parameters = new
            {
                type = "object",
                properties = new { },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto GetRestockSuggestions => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_restock_suggestions",
            Description = "Get intelligent restocking suggestions based on sales velocity and historical trends.",
            Parameters = new
            {
                type = "object",
                properties = new { },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto GetFastSellingProducts => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_fast_selling_products",
            Description = "Get products that are selling quickly and may need attention.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    days = new { type = "integer", description = "Number of days to analyze. Default: 7." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Helper: Get tools by domain
    // ═══════════════════════════════════════════════════

    public static List<GroqToolDto> GetShoppingTools() => new()
    {
        SearchProducts, AddToCart, UpdateQuantity, RemoveFromCart, GetCart, FindBestStore
    };

    public static List<GroqToolDto> GetCustomerSupportTools() => new()
    {
        TrackOrder, GetDeliveryEta, GetRefundStatus, GetOrderDetails
    };

    public static List<GroqToolDto> GetOrderTrackingTools() => new()
    {
        TrackOrder, GetDeliveryEta
    };

    public static List<GroqToolDto> GetRecommendationTools() => new()
    {
        GetRecommendations
    };

    public static List<GroqToolDto> GetShopOwnerProductTools() => new()
    {
        GenerateProductDescription, GenerateSeoKeywords
    };

    public static List<GroqToolDto> GetShopOwnerReportingTools() => new()
    {
        GenerateReport
    };

    public static List<GroqToolDto> GetShopOwnerAnalyticsTools() => new()
    {
        AnalyzeSalesTrend
    };

    public static GroqToolDto GetInventoryHealthScore => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_inventory_health_score",
            Description = "Get overall inventory health score including percentage of products at risk, overstocked, and balanced. Provides a health rating (Excellent, Good, Fair, Needs Attention).",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetSeasonalDemandForecast => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_seasonal_demand_forecast",
            Description = "Compare current period sales with the same period last year to predict seasonal demand shifts and recommend stock adjustments.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Marketing Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetMarketingInsights => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_marketing_insights",
            Description = "Get marketing performance insights including top performing products, revenue trends (month-over-month), and customer acquisition data (new vs returning).",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto SuggestPromotions => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "suggest_promotions",
            Description = "Analyze slow-moving inventory (products with low sales over 90 days) and suggest discount strategies and promotional campaigns.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto SuggestBundles => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "suggest_bundles",
            Description = "Identify frequently co-purchased products and suggest bundle deals with recommended discount pricing (15% off combined price).",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Customer Insight Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetCustomerBehaviorAnalysis => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_customer_behavior_analysis",
            Description = "Analyze customer behavior including new vs returning customers, purchase frequency, average order value, and customer segments by spend.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetCustomerSatisfactionStats => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_customer_satisfaction_stats",
            Description = "Get customer satisfaction metrics including review rating distribution (1-5 stars), average rating, common complaint keywords, and satisfaction trend over time.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetTopCustomers => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_top_customers",
            Description = "Get the highest-spending and most frequent customers, including their total spend, order count, average order value, and customer lifetime.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Helper: Get tools by domain
    // ═══════════════════════════════════════════════════

    public static List<GroqToolDto> GetShopOwnerInventoryTools() => new()
    {
        CheckLowStock, GetRestockSuggestions, GetFastSellingProducts, GetInventoryHealthScore, GetSeasonalDemandForecast
    };

    public static List<GroqToolDto> GetMarketingTools() => new()
    {
        GetMarketingInsights, SuggestPromotions, SuggestBundles
    };

    public static List<GroqToolDto> GetCustomerInsightTools() => new()
    {
        GetCustomerBehaviorAnalysis, GetCustomerSatisfactionStats, GetTopCustomers
    };

    // ═══════════════════════════════════════════════════
    //  Admin: Fraud Detection Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto DetectSuspiciousOrders => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "detect_suspicious_orders",
            Description = "Scan recent orders for fraud patterns: order velocity, amount anomalies, rapid cancellations, refund abuse, new account bulk orders.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    days = new { type = "integer", description = "Number of days to scan. Default: 7." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto DetectDuplicateAccounts => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "detect_duplicate_accounts",
            Description = "Find accounts sharing suspicious similarities (same email prefix, same IP, same phone pattern).",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetFraudSummary => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_fraud_summary",
            Description = "Get fraud alert statistics and trends, grouped by type, severity, and status.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    startDate = new { type = "string", description = "Optional start date (ISO format)." },
                    endDate = new { type = "string", description = "Optional end date (ISO format)." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Admin: Security Monitoring Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetSecurityEvents => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_security_events",
            Description = "Query security event logs (failed logins, unauthorized access, rate limited).",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    days = new { type = "integer", description = "Number of days to look back. Default: 7." },
                    severity = new { type = "string", description = "Optional filter: low, medium, high, critical." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto GetLoginAnomalies => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_login_anomalies",
            Description = "Identify suspicious login patterns: users with high failed login counts and locked accounts.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    days = new { type = "integer", description = "Number of days to analyze. Default: 7." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto GetRateLimitViolations => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_rate_limit_violations",
            Description = "Get rate-limited request stats, grouped by IP address.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    days = new { type = "integer", description = "Number of days to analyze. Default: 7." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Admin: Platform Analytics Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetPlatformOverview => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_platform_overview",
            Description = "Get cross-platform metrics: total GMV, orders, users, stores, active users, new users this month.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetStoreRankings => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_store_rankings",
            Description = "Rank stores by revenue, order count, customer rating, or growth rate.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    metric = new { type = "string", description = "Metric to rank by: 'revenue', 'orders', 'rating', 'growth'. Default: 'revenue'." },
                    top = new { type = "integer", description = "Number of top stores to return. Default: 10." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto GetCategoryTrends => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_category_trends",
            Description = "Analyze category performance: revenue, order count, and growth rate per category.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    days = new { type = "integer", description = "Number of days to analyze. Default: 30." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto GetRegionalAnalytics => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_regional_analytics",
            Description = "Get geographic distribution of stores, customers, and revenue by region/country.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Admin: Store Approval Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetPendingStores => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_pending_stores",
            Description = "List all store profiles for review, ordered by creation date.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetStoreRiskScore => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_store_risk_score",
            Description = "Calculate risk score for a specific store based on profile completeness, verification, and product quality.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    storeUserId = new { type = "string", description = "The store owner's user ID." }
                },
                required = new[] { "storeUserId" }
            }
        }
    };

    public static GroqToolDto GetStoreComplianceCheck => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_store_compliance_check",
            Description = "Run compliance verification for a store: email, phone, description, location, products, rapid uploads.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    storeUserId = new { type = "string", description = "The store owner's user ID." }
                },
                required = new[] { "storeUserId" }
            }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Admin: Moderation Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetFlaggedContent => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_flagged_content",
            Description = "Get flagged products and reviews for moderation review.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    contentType = new { type = "string", description = "Optional filter: 'product' or 'review'." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto GetReviewModeration => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_review_moderation",
            Description = "Analyze review authenticity: detect rapid reviews, identical text across products, and patterns from new accounts.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetPricingAnomalies => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_pricing_anomalies",
            Description = "Detect pricing outliers: products priced 10x higher or lower than category average, and products with placeholder content.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Admin: Global Support Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetEscalatedIssues => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_escalated_issues",
            Description = "Get conversations that have been escalated to human support.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetCrossStoreDisputes => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_cross_store_disputes",
            Description = "Find disputes involving orders that span multiple stores.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetUserHistory => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_user_history",
            Description = "Get full user profile and activity: account age, orders, cancellation rate, feedback, AI conversations.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    userId = new { type = "string", description = "The user ID to look up." }
                },
                required = new[] { "userId" }
            }
        }
    };

    // ═══════════════════════════════════════════════════
    //  Helper: Admin tool groups
    // ═══════════════════════════════════════════════════

    public static List<GroqToolDto> GetFraudDetectionTools() => new()
    {
        DetectSuspiciousOrders, DetectDuplicateAccounts, GetFraudSummary
    };

    public static List<GroqToolDto> GetSecurityMonitoringTools() => new()
    {
        GetSecurityEvents, GetLoginAnomalies, GetRateLimitViolations
    };

    public static List<GroqToolDto> GetPlatformAnalyticsTools() => new()
    {
        GetPlatformOverview, GetStoreRankings, GetCategoryTrends, GetRegionalAnalytics
    };

    public static List<GroqToolDto> GetStoreApprovalTools() => new()
    {
        GetPendingStores, GetStoreRiskScore, GetStoreComplianceCheck
    };

    public static List<GroqToolDto> GetModerationTools() => new()
    {
        GetFlaggedContent, GetReviewModeration, GetPricingAnomalies
    };

    public static List<GroqToolDto> GetGlobalSupportTools() => new()
    {
        GetEscalatedIssues, GetCrossStoreDisputes, GetUserHistory
    };

    // ═══════════════════════════════════════════════════
    //  Business Advisor Tools
    // ═══════════════════════════════════════════════════

    public static GroqToolDto GetStoreInspection => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_store_inspection",
            Description = "Inspect the store's current state. Returns product quality metrics, revenue trends, inventory health, review scores, and category coverage. Always call this first before making any advisor decisions.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetBusinessGoals => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_business_goals",
            Description = "Get all business goals set by the store owner, including current progress and status.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto GetRecommendationHistory => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_recommendation_history",
            Description = "Get the history of business recommendations for this store, including status (Pending, InProgress, Completed, Skipped, Expired). Use this to verify what was previously recommended and avoid repeats.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    count = new { type = "integer", description = "Number of recent recommendations to retrieve. Default: 20." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto GetBusinessHealthScore => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_business_health_score",
            Description = "Calculate the current Business Health Score (0-100) with a breakdown by factor: revenue trend, order growth, inventory health, product quality, review score, category completeness, SEO completeness, image completeness.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }
    };

    public static GroqToolDto CreateRecommendation => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "create_recommendation",
            Description = "Create a new business recommendation for the store owner. Only create after verifying that previous pending recommendations are completed or expired. Every recommendation must be based on real store data.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    title = new { type = "string", description = "Short title for the recommendation." },
                    description = new { type = "string", description = "Detailed description of what the merchant should do." },
                    reason = new { type = "string", description = "Data-driven reason why this recommendation matters." },
                    priority = new { type = "string", description = "Priority level: Low, Medium, High, or Critical.", @enum = new[] { "Low", "Medium", "High", "Critical" } },
                    verificationCriteria = new { type = "string", description = "How to verify if this recommendation was completed." },
                    goalId = new { type = "integer", description = "Optional ID of the business goal this supports." }
                },
                required = new[] { "title", "description", "reason", "priority" }
            }
        }
    };

    public static GroqToolDto UpdateRecommendationStatus => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "update_recommendation_status",
            Description = "Update the status of an existing recommendation. Use after verifying whether the merchant completed, skipped, or let a recommendation expire.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    recommendationId = new { type = "integer", description = "The ID of the recommendation to update." },
                    status = new { type = "string", description = "New status: Completed, Skipped, or Expired.", @enum = new[] { "Completed", "Skipped", "Expired" } },
                    verificationResult = new { type = "string", description = "Description of what was verified." }
                },
                required = new[] { "recommendationId", "status" }
            }
        }
    };

    public static GroqToolDto GetAdvisorMemories => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "get_advisor_memories",
            Description = "Retrieve past advisor observations, strategies, milestones, and context for this store. Use to recall what was previously observed and maintain continuity.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    count = new { type = "integer", description = "Number of memories to retrieve. Default: 30." }
                },
                required = Array.Empty<string>()
            }
        }
    };

    public static GroqToolDto SaveAdvisorMemory => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "save_advisor_memory",
            Description = "Save an important observation or strategy for future sessions. Use for insights that should persist across days (e.g., 'Store improved descriptions on July 5', 'Revenue is trending upward since last week').",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    memoryType = new { type = "string", description = "Type of memory: Strategy, Observation, Milestone, or Context.", @enum = new[] { "Strategy", "Observation", "Milestone", "Context" } },
                    content = new { type = "string", description = "The memory content to persist." },
                    importance = new { type = "integer", description = "Importance from 1 (low) to 10 (critical). Default: 5." }
                },
                required = new[] { "memoryType", "content" }
            }
        }
    };

    public static GroqToolDto CreateBusinessGoal => new()
    {
        Type = "function",
        Function = new GroqFunctionDto
        {
            Name = "create_business_goal",
            Description = "Create a new business goal for the store owner. Use when the merchant expresses a desire to improve their store or reach a target.",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    title = new { type = "string", description = "Short goal title, e.g. 'Increase monthly revenue by 20%'." },
                    description = new { type = "string", description = "Detailed description of the goal." },
                    targetMetric = new { type = "string", description = "The metric being targeted: revenue, orders, conversion, etc." },
                    targetValue = new { type = "number", description = "The target value for the metric." }
                },
                required = new[] { "title" }
            }
        }
    };

    public static List<GroqToolDto> GetBusinessAdvisorTools() => new()
    {
        GetStoreInspection, GetBusinessGoals, GetRecommendationHistory,
        GetBusinessHealthScore, CreateRecommendation, UpdateRecommendationStatus,
        GetAdvisorMemories, SaveAdvisorMemory, CreateBusinessGoal
    };
}
