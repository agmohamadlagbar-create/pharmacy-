using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using OnlineStore.Api.Hubs;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using OnlineStore.Infrastructure.Persistence;
using HeyRed.ImageSharp.Heif.Formats.Heif;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using QuestPDF.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/ecommerce_ai_log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

// Stripe Configuration
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe:SecretKey missing");

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR
builder.Services.AddSignalR()
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Db + Identity (SQL connection pooling via connection string: Max Pool Size=200)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(3));
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return System.Threading.Tasks.Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return System.Threading.Tasks.Task.CompletedTask;
    };
});

// JWT Auth
// IMPORTANT: ASP.NET Core 8 + JWT 7.x uses JsonWebTokenHandler internally, which ALWAYS
// remaps JWT claim types: "sub" → ClaimTypes.NameIdentifier, "role" → ClaimTypes.Role.
// The NameClaimType/RoleClaimType must match the REMAPPED (long) claim URIs.
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes for production security.");
}
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(5),
            // Use the LONG claim type URIs that JsonWebTokenHandler maps to:
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        // Allow SignalR to receive JWT from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// App services
builder.Services.AddScoped<OnlineStore.Api.Services.IJwtTokenService, OnlineStore.Api.Services.JwtTokenService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IEmailService, OnlineStore.Api.Services.SmtpEmailService>();
builder.Services.AddSingleton<OnlineStore.Api.Services.IVerificationCodeStore, OnlineStore.Api.Services.InMemoryVerificationCodeStore>();
builder.Services.AddScoped<OnlineStore.Api.Services.INotificationService, OnlineStore.Api.Services.NotificationService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IFileStorageService, OnlineStore.Api.Services.FileStorageService>();
builder.Services.AddSingleton<OnlineStore.Api.Services.IImageProcessingQueue, OnlineStore.Api.Services.ImageProcessingQueue>();
builder.Services.AddHostedService<OnlineStore.Api.Services.ImageProcessingBackgroundService>();
builder.Services.AddHostedService<OnlineStore.Api.Services.NotificationCleanupService>();
builder.Services.AddHostedService<OnlineStore.Api.Services.DeliveredOrderCleanupService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<OnlineStore.Api.Services.ICacheService, OnlineStore.Api.Services.LocalCacheService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IStoreCacheService, OnlineStore.Api.Services.StoreCacheService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IProductSearchService, OnlineStore.Api.Services.ProductSearchService>();
builder.Services.AddScoped<OnlineStore.Api.Services.ICartService, OnlineStore.Api.Services.CartService>();
builder.Services.AddScoped<OnlineStore.Api.Services.ICustomerSupportService, OnlineStore.Api.Services.CustomerSupportService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IChatHistoryService, OnlineStore.Api.Services.ChatHistoryService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IStoreSelectionService, OnlineStore.Api.Services.StoreSelectionService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IRecommendationService, OnlineStore.Api.Services.RecommendationService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IAiOrchestratorService, OnlineStore.Api.Services.AiOrchestratorService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IAnalyticsService, OnlineStore.Api.Services.AnalyticsService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IReportExportService, OnlineStore.Api.Services.ReportExportService>();

// Phase 4: Feedback & Learning System
builder.Services.AddScoped<OnlineStore.Api.Services.IFeedbackService, OnlineStore.Api.Services.FeedbackService>();
builder.Services.AddScoped<OnlineStore.Api.Services.ILearningService, OnlineStore.Api.Services.LearningService>();
builder.Services.AddHostedService<OnlineStore.Api.Services.LearningBackgroundService>();

// Advanced Memory & A/B Testing
builder.Services.AddScoped<OnlineStore.Api.Services.IAiMemoryService, OnlineStore.Api.Services.AiMemoryService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IAbTestingService, OnlineStore.Api.Services.AbTestingService>();
builder.Services.AddHostedService<OnlineStore.Api.Services.MemoryCleanupBackgroundService>();

// Telegram services
builder.Services.AddHttpClient("TelegramBot");
builder.Services.AddScoped<OnlineStore.Api.Services.ITelegramService, OnlineStore.Api.Services.TelegramService>();
builder.Services.AddHostedService<OnlineStore.Api.Services.TelegramNotificationBackgroundService>();

builder.Services.AddHttpClient<OnlineStore.Api.Services.IGroqAiService, OnlineStore.Api.Services.GroqAiService>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    var apiKey = builder.Configuration["Groq:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }
});

// ═══════════════════════════════════════════════════
//  Multi-Provider AI Gateway
// ═══════════════════════════════════════════════════

// Configuration
builder.Services.Configure<OnlineStore.Api.Services.Agents.AiProviderConfig>(
    builder.Configuration.GetSection("AiGateway"));

// Groq API Client (primary provider)
builder.Services.AddHttpClient<OnlineStore.Api.Services.Agents.GroqApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    var apiKey = builder.Configuration["Groq:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }
});

// GLM API Client (secondary provider — OpenAI-compatible)
builder.Services.AddHttpClient<OnlineStore.Api.Services.Agents.GlmApiClient>(client =>
{
    var baseUrl = builder.Configuration["Glm:BaseUrl"]
        ?? "https://open.bigmodel.cn/api/paas/v4/";
    client.BaseAddress = new Uri(baseUrl);
    var apiKey = builder.Configuration["Glm:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }
});

// FreeLLMAPI Client (OpenAI-compatible gateway)
builder.Services.AddHttpClient<OnlineStore.Api.Services.Agents.FreeLlmApiClient>(client =>
{
    var baseUrl = builder.Configuration["FreeLlmApi:BaseUrl"]
        ?? "http://localhost:3001/v1/";
    client.BaseAddress = new Uri(baseUrl);
    var apiKey = builder.Configuration["FreeLlmApi:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }
});

// Usage Tracker (singleton — shared counter state across all requests)
builder.Services.AddSingleton<OnlineStore.Api.Services.Agents.AiUsageTracker>();
builder.Services.AddSingleton<OnlineStore.Api.Services.IConversationStateManager, OnlineStore.Api.Services.ConversationStateManager>();

// AI Gateway (scoped — one per request)
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.AiGateway>();

// Tool Registry (singleton — registered once, used by all agents)
builder.Services.AddSingleton<OnlineStore.Api.Services.Agents.ToolRegistry>();

// Customer Agents
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Customer.PlannerAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Customer.ShoppingAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Customer.CustomerServiceAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Customer.RecommendationAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Customer.OrderTrackingAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Customer.ReflectionAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Customer.GeneralChatAgent>();

// Shop Owner Agents
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.ShopOwner.ShopOwnerPlannerAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.ShopOwner.ProductManagementAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.ShopOwner.ReportingAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.ShopOwner.AnalyticsAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.ShopOwner.InventoryAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.ShopOwner.MarketingAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.ShopOwner.CustomerInsightAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.ShopOwner.BusinessAdvisorAgent>();

// Shop Owner Backend Services
builder.Services.AddScoped<OnlineStore.Api.Services.IProductManagementAiService, OnlineStore.Api.Services.ProductManagementAiService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IInventoryIntelligenceService, OnlineStore.Api.Services.InventoryIntelligenceService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IMarketingService, OnlineStore.Api.Services.MarketingService>();
builder.Services.AddScoped<OnlineStore.Api.Services.ICustomerInsightService, OnlineStore.Api.Services.CustomerInsightService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IBusinessAdvisorService, OnlineStore.Api.Services.BusinessAdvisorService>();
builder.Services.AddScoped<OnlineStore.Api.Services.BusinessAdvisorJobs>();

// Hangfire services
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());
builder.Services.AddHangfireServer();

// Admin Agents
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Admin.AdminPlannerAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Admin.FraudDetectionAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Admin.SecurityMonitoringAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Admin.PlatformAnalyticsAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Admin.StoreApprovalAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Admin.ModerationAgent>();
builder.Services.AddScoped<OnlineStore.Api.Services.Agents.Admin.GlobalSupportAgent>();

// Admin Backend Services
builder.Services.AddScoped<OnlineStore.Api.Services.IFraudDetectionService, OnlineStore.Api.Services.FraudDetectionService>();
builder.Services.AddScoped<OnlineStore.Api.Services.ISecurityMonitoringService, OnlineStore.Api.Services.SecurityMonitoringService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IPlatformAnalyticsService, OnlineStore.Api.Services.PlatformAnalyticsService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IStoreApprovalService, OnlineStore.Api.Services.StoreApprovalService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IModerationService, OnlineStore.Api.Services.ModerationService>();
builder.Services.AddScoped<OnlineStore.Api.Services.IGlobalSupportService, OnlineStore.Api.Services.GlobalSupportService>();

// Security Event Middleware (singleton channel + background service)
builder.Services.AddSingleton<OnlineStore.Api.Helpers.SecurityEventChannel>();
builder.Services.AddHostedService<OnlineStore.Api.Helpers.SecurityEventBackgroundService>();

// Configure ImageSharp for HEIF support
SixLabors.ImageSharp.Configuration.Default.Configure(new HeifConfigurationModule());

// CORS for React dev server + Flutter
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true);
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // AI Endpoints Rate Limiter: Per User or IP
    options.AddPolicy("AiRateLimit", httpContext =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = !string.IsNullOrEmpty(userId) ? userId : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 15, // 15 requests per minute
                Window = TimeSpan.FromMinutes(1)
            });
    });

    // Auth Endpoints Rate Limiter: Per IP (unauthenticated requests)
    options.AddPolicy("AuthRateLimit", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter($"auth_{partitionKey}", _ =>
            new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5, // 5 login attempts per minute per IP
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseResponseCompression();

// Only log at Debug level, not on every request
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogDebug("[Request] {Method} {Path}", context.Request.Method, context.Request.Path);
        await next();
    });
}

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            // Disable caching during development so changes are immediately visible
            ctx.Context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
        }
        else
        {
            // Cache static files (like CSS, JS plugins, etc.) for 1 year in production
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
        }
    }
});

// Serve images from /storage/ folder via /media/ URL path
var storageRoot = builder.Configuration["StorageRoot"] ?? "storage";
var storageFullPath = Path.IsPathRooted(storageRoot)
    ? storageRoot
    : Path.Combine(app.Environment.ContentRootPath, storageRoot);
if (!Directory.Exists(storageFullPath)) Directory.CreateDirectory(storageFullPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageFullPath),
    RequestPath = "/media",
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-store");
        }
        else
        {
            // Cache media resources (like images) for 1 day in production
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=86400");
        }
    }
});

// Explicitly serve the mobile-app folder if needed (though it is in wwwroot)
var mobileAppPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "mobile-app");
if (!Directory.Exists(mobileAppPath)) Directory.CreateDirectory(mobileAppPath);

app.UseCors("dev");

app.UseRateLimiter();

// Security event middleware
app.UseMiddleware<OnlineStore.Api.Helpers.SecurityEventMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AiChatHub>("/hubs/ai-chat").RequireRateLimiting("AiRateLimit");
app.MapHub<OnlineStore.Api.Hubs.ShopAiChatHub>("/hubs/shop-ai-chat").RequireRateLimiting("AiRateLimit");
app.MapHub<OnlineStore.Api.Hubs.AdminAiChatHub>("/hubs/admin-ai-chat").RequireRateLimiting("AiRateLimit");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<OnlineStore.Api.Hubs.TrackingHub>("/hubs/tracking");

// Register all tools in the ToolRegistry
var toolRegistry = app.Services.GetRequiredService<OnlineStore.Api.Services.Agents.ToolRegistry>();
OnlineStore.Api.Services.Agents.ToolRegistrationService.RegisterAllTools(toolRegistry, app.Services);

// Auto-create roles + seed Admin on startup
var isDesignTime = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name?.Contains("ef", StringComparison.OrdinalIgnoreCase) == true
    || AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name?.Contains("Design", StringComparison.OrdinalIgnoreCase) == true);

if (!isDesignTime)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            db.Database.Migrate();
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "User", "Shop", "Delivery", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed Admin account
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var adminEmail = Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL") ?? "admin@lagbar.com";
        var adminPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                AccountType = OnlineStore.Domain.Entities.AccountType.Admin,
                FullName = "LAGBAR Admin"
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            adminPassword = string.Empty;
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                Console.WriteLine($"✅ Admin account created: {adminEmail} / {adminPassword}");
            }
            else
            {
                Console.WriteLine($"⚠️ Failed to create admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else if (existingAdmin == null)
        {
            Console.WriteLine("Admin account seed skipped. Set SEED_ADMIN_PASSWORD to create it.");
        }
        else
        {
            Console.WriteLine($"ℹ️ Admin account already exists: {adminEmail}");
        }

        // Seed default categories if none exist
        if (!db.Categories.Any())
        {
            var defaultCategories = new[]
            {
                new OnlineStore.Domain.Entities.Category { Name = "Food", Code = "FOOD", ImageUrl = "/uploads/category_food.png" },
                new OnlineStore.Domain.Entities.Category { Name = "Beverages", Code = "BEV", ImageUrl = "/uploads/category_beverages.png" },
                new OnlineStore.Domain.Entities.Category { Name = "Cleaning Supplies", Code = "CLEAN", ImageUrl = "/uploads/category_cleaning.png" },
                new OnlineStore.Domain.Entities.Category { Name = "House Needs", Code = "HOUSE", ImageUrl = "/uploads/category_house.png" },
                new OnlineStore.Domain.Entities.Category { Name = "Electronics", Code = "ELEC", ImageUrl = "/uploads/category_electronics.png" },
                new OnlineStore.Domain.Entities.Category { Name = "Clothing", Code = "CLOTH", ImageUrl = "/uploads/category_clothing.png" },
            };
            db.Categories.AddRange(defaultCategories);
            await db.SaveChangesAsync();
            Console.WriteLine($"✅ Seeded {defaultCategories.Length} default categories.");
        }

        // Update existing shop profiles to have a default RegionCode if null
        var shopsToUpdate = await db.ShopProfiles.Where(s => s.RegionCode == null).ToListAsync();
        if (shopsToUpdate.Any())
        {
            foreach (var shop in shopsToUpdate)
            {
                shop.RegionCode = "GLOBAL";
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"✅ Updated {shopsToUpdate.Count} shop profiles with default RegionCode 'GLOBAL'.");
        }
    }
}

// Hangfire Dashboard and recurring jobs
app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<OnlineStore.Api.Services.BusinessAdvisorJobs>(
    "business-advisor-daily",
    job => job.GenerateDailyReportsAsync(),
    Cron.Daily(9, 0));

RecurringJob.AddOrUpdate<OnlineStore.Api.Services.BusinessAdvisorJobs>(
    "business-advisor-weekly",
    job => job.GenerateWeeklyReportsAsync(),
    "0 18 * * 0"); // Sunday 18:00

RecurringJob.AddOrUpdate<OnlineStore.Api.Services.BusinessAdvisorJobs>(
    "business-advisor-monthly",
    job => job.GenerateMonthlyReportsAsync(),
    "0 9 1 * *"); // First of month 09:00

// SPA fallback: serve index.html for any route not handled by API or static files
app.MapFallbackToFile("index.html");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
