using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Auth;
using System.Text.Json;

namespace OnlineStore.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ShopProfile> ShopProfiles => Set<ShopProfile>();
    public DbSet<DeliveryProfile> DeliveryProfiles => Set<DeliveryProfile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<DeliveryJob> DeliveryJobs => Set<DeliveryJob>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AdBanner> AdBanners => Set<AdBanner>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<UserImage> UserImages => Set<UserImage>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<AiToolCall> AiToolCalls => Set<AiToolCall>();
    public DbSet<AiFeedback> AiFeedbacks => Set<AiFeedback>();
    public DbSet<AiConversationAnalysis> AiConversationAnalyses => Set<AiConversationAnalysis>();
    public DbSet<AiKnowledgeGap> AiKnowledgeGaps => Set<AiKnowledgeGap>();
    public DbSet<AiImprovementSuggestion> AiImprovementSuggestions => Set<AiImprovementSuggestion>();
    public DbSet<AiPromptVariant> AiPromptVariants => Set<AiPromptVariant>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<FraudAlert> FraudAlerts => Set<FraudAlert>();
    public DbSet<ModerationFlag> ModerationFlags => Set<ModerationFlag>();
    public DbSet<AiUserPreference> AiUserPreferences => Set<AiUserPreference>();
    public DbSet<AiShopContext> AiShopContexts => Set<AiShopContext>();
    public DbSet<AiLongTermMemory> AiLongTermMemories => Set<AiLongTermMemory>();
    public DbSet<AiEpisodicMemory> AiEpisodicMemories => Set<AiEpisodicMemory>();

    // Business Advisor
    public DbSet<BusinessGoal> BusinessGoals => Set<BusinessGoal>();
    public DbSet<BusinessRecommendation> BusinessRecommendations => Set<BusinessRecommendation>();
    public DbSet<BusinessAdvisorReport> BusinessAdvisorReports => Set<BusinessAdvisorReport>();
    public DbSet<BusinessAdvisorMemory> BusinessAdvisorMemories => Set<BusinessAdvisorMemory>();

    // Store Analytics Aggregations & Events
    public DbSet<StoreAnalyticsEvent> StoreAnalyticsEvents => Set<StoreAnalyticsEvent>();
    public DbSet<StoreAnalyticsDaily> StoreAnalyticsDailies => Set<StoreAnalyticsDaily>();
    public DbSet<StoreAnalyticsMonthly> StoreAnalyticsMonthlies => Set<StoreAnalyticsMonthly>();
    public DbSet<StoreAnalyticsYearly> StoreAnalyticsYearlies => Set<StoreAnalyticsYearly>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ShopProfile
        builder.Entity<ShopProfile>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ShopName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Location).HasMaxLength(400).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.OwnerName).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasIndex(x => x.UserId).IsUnique();
            b.Property(x => x.LowStockThreshold).HasDefaultValue(10);
        });

        // DeliveryProfile
        builder.Entity<DeliveryProfile>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.VehicleInfo).HasMaxLength(200);
            b.HasIndex(x => x.UserId).IsUnique();
        });

        // Category
        builder.Entity<Category>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.ImageUrl).HasMaxLength(500);
            b.HasIndex(x => x.Name).IsUnique();
            b.HasIndex(x => x.ParentId);
        });

        // Product
        builder.Entity<Product>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Price).HasColumnType("decimal(18,2)");
            b.Property(x => x.ImageUrl).HasMaxLength(500);
            b.Property(x => x.RejectionReason).HasMaxLength(1000);

            b.Property(x => x.AdditionalImageUrls)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => SafeDeserializeAdditionalImageUrls(v)
                );

            b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => x.Price);
        });

        // Review
        builder.Entity<Review>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.UserName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Comment).HasMaxLength(1500);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.UserId);
        });

        // Order
        builder.Entity<Order>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            b.Property(x => x.DeliveryAddress).HasMaxLength(500);
            b.Property(x => x.OrderGroupId).HasMaxLength(100);
            b.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => x.OrderGroupId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CreatedAtUtc);
        });

        // OrderItem
        builder.Entity<OrderItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.OrderId);
        });

        // DeliveryJob
        builder.Entity<DeliveryJob>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.DeliveryUserId).HasMaxLength(450);
            b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.OrderId).IsUnique();
            b.HasIndex(x => x.DeliveryUserId);
            b.HasIndex(x => x.Status);
        });

        // RefreshToken
        builder.Entity<RefreshToken>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.TokenHash);
        });

        // Notification
        builder.Entity<Notification>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.IsRead });
            b.HasIndex(x => x.CreatedAtUtc);
        });

        // AdBanner
        builder.Entity<AdBanner>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.LinkUrl).HasMaxLength(500);
        });

        // Cart
        builder.Entity<Cart>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.HasMany(x => x.Items).WithOne(x => x.Cart).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.UserId).IsUnique();
        });

        // CartItem
        builder.Entity<CartItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // ProductImage
        builder.Entity<ProductImage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Country).HasMaxLength(100).IsRequired();
            b.Property(x => x.City).HasMaxLength(100).IsRequired();
            b.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.ImageType).HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active");
            b.Property(x => x.UploadedBy).HasMaxLength(450).IsRequired();
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.StorageKey);
        });

        // UserImage
        builder.Entity<UserImage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Country).HasMaxLength(100).IsRequired();
            b.Property(x => x.City).HasMaxLength(100).IsRequired();
            b.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
            b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.ImageType).HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active");
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.StorageKey);
        });

        // UserAddress
        builder.Entity<UserAddress>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Title).HasMaxLength(150);
            b.Property(x => x.Street).HasMaxLength(250);
            b.Property(x => x.BuildingNumber).HasMaxLength(50);
            b.Property(x => x.City).HasMaxLength(100);
            b.Property(x => x.PostalCode).HasMaxLength(20);
            b.HasIndex(x => x.UserId);
        });

        // AiConversation
        builder.Entity<AiConversation>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Title).HasMaxLength(300);
            b.Property(x => x.ModelName).HasMaxLength(120);
            b.Property(x => x.SystemPrompt).HasMaxLength(8000);
            b.Property(x => x.AgentType).HasMaxLength(50);
            b.Property(x => x.LastAgentUsed).HasMaxLength(120);
            b.HasMany(x => x.Messages).WithOne(x => x.Conversation).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.ToolCalls).WithOne(x => x.Conversation).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            b.OwnsOne(x => x.State, sb =>
            {
                sb.Property(s => s.PendingProductId);
                sb.Property(s => s.PendingProductName).HasMaxLength(300);
                sb.Property(s => s.PendingQuantity);
                sb.Property(s => s.PendingAction).HasMaxLength(100);
                sb.Property(s => s.AwaitingConfirmation);
                sb.Property(s => s.PendingIntentJson).HasMaxLength(2000);
                sb.Property(s => s.LastShoppingIntentJson).HasMaxLength(2000);
                sb.Property(s => s.SelectedProductId);
                sb.Property(s => s.SelectedStoreName).HasMaxLength(200);
                sb.Property(s => s.LastSuccessfulToolResult).HasMaxLength(4000);
            });
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.UpdatedAtUtc });
        });

        // AiMessage
        builder.Entity<AiMessage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.SequenceNumber).IsRequired();
            b.Property(x => x.Role).HasMaxLength(20).IsRequired();
            b.Property(x => x.Content).IsRequired();
            b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.ConversationId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.ConversationId, x.SequenceNumber }).IsUnique();
            b.HasIndex(x => new { x.ConversationId, x.UserId });
            b.ToTable(t => t.HasCheckConstraint("CK_AiMessages_Role", "[Role] IN ('user','assistant','system','tool')"));
        });

        // AiToolCall
        builder.Entity<AiToolCall>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ToolCallId).HasMaxLength(120).IsRequired();
            b.Property(x => x.ToolName).HasMaxLength(120).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.ErrorMessage).HasMaxLength(2000);
            b.HasOne(x => x.Message).WithMany(x => x.ToolCalls).HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.NoAction);
            b.HasIndex(x => x.ConversationId);
            b.HasIndex(x => x.MessageId);
            b.HasIndex(x => new { x.ConversationId, x.StartedAtUtc });
            b.HasIndex(x => new { x.ConversationId, x.ToolCallId }).IsUnique();
            b.ToTable(t => t.HasCheckConstraint("CK_AiToolCalls_Status", "[Status] IN ('Requested','Succeeded','Failed')"));
        });

        // AiFeedback
        builder.Entity<AiFeedback>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.FeedbackType).HasMaxLength(50);
            b.Property(x => x.ImplicitSignal).HasMaxLength(50);
            b.Property(x => x.AgentName).HasMaxLength(120);
            b.Property(x => x.FeedbackSource).HasMaxLength(20).IsRequired();
            b.Property(x => x.Comment).HasMaxLength(2000);
            b.HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Message).WithMany().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.ClientSetNull);
            b.HasIndex(x => new { x.ConversationId, x.CreatedAtUtc });
            b.HasIndex(x => x.AgentName);
            b.HasIndex(x => x.UserId);
        });

        // AiConversationAnalysis
        builder.Entity<AiConversationAnalysis>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.AnalysisDate).IsRequired();
            b.HasIndex(x => x.AnalysisDate).IsUnique();
        });

        // AiKnowledgeGap
        builder.Entity<AiKnowledgeGap>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Topic).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.Resolution).HasMaxLength(2000);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.Topic);
        });

        // AiImprovementSuggestion
        builder.Entity<AiImprovementSuggestion>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Category).HasMaxLength(50).IsRequired();
            b.Property(x => x.AgentName).HasMaxLength(120).IsRequired();
            b.Property(x => x.Title).HasMaxLength(300).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.ReviewedBy).HasMaxLength(450);
            b.Property(x => x.ReviewNotes).HasMaxLength(2000);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.AgentName);
        });

        // AiPromptVariant
        builder.Entity<AiPromptVariant>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.AgentName).HasMaxLength(120).IsRequired();
            b.Property(x => x.VariantName).HasMaxLength(200).IsRequired();
            b.Property(x => x.SystemPrompt).IsRequired();
            b.Property(x => x.ExperimentName).HasMaxLength(200);
            b.Property(x => x.WinnerDecision).HasMaxLength(30);
            b.HasIndex(x => new { x.AgentName, x.VariantName }).IsUnique();
            b.HasIndex(x => new { x.AgentName, x.IsActive });
            b.HasIndex(x => x.ExperimentName);
        });

        // AiUserPreference
        builder.Entity<AiUserPreference>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.PreferenceKey).HasMaxLength(100).IsRequired();
            b.Property(x => x.PreferenceValue).HasMaxLength(500).IsRequired();
            b.Property(x => x.Source).HasMaxLength(20).IsRequired();
            b.HasIndex(x => new { x.UserId, x.PreferenceKey }).IsUnique();
        });

        // AiShopContext
        builder.Entity<AiShopContext>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.PreferredLanguage).HasMaxLength(10);
            b.Property(x => x.PreferredCurrency).HasMaxLength(10);
            b.Property(x => x.ShopPoliciesJson).HasMaxLength(4000);
            b.Property(x => x.ReportingPreferencesJson).HasMaxLength(2000);
            b.Property(x => x.CustomInstructionsJson).HasMaxLength(2000);
            b.HasIndex(x => x.UserId).IsUnique();
        });

        // AiLongTermMemory
        builder.Entity<AiLongTermMemory>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.MemoryType).HasMaxLength(30).IsRequired();
            b.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            b.Property(x => x.SourceConversationSummary).HasMaxLength(500);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.MemoryType });
            b.HasIndex(x => x.ExpiresAtUtc);
        });

        // AiEpisodicMemory
        builder.Entity<AiEpisodicMemory>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.EpisodeType).HasMaxLength(50).IsRequired();
            b.Property(x => x.Summary).HasMaxLength(2000).IsRequired();
            b.Property(x => x.Outcome).HasMaxLength(30);
            b.Property(x => x.SentimentAtEnd).HasMaxLength(20);
            b.Property(x => x.KeyEntitiesJson).HasMaxLength(2000);
            b.HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.EpisodeType });
        });

        // Phase 5: SecurityEvent
        builder.Entity<SecurityEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).HasMaxLength(50).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(450);
            b.Property(x => x.IpAddress).HasMaxLength(50);
            b.Property(x => x.UserAgent).HasMaxLength(500);
            b.Property(x => x.RequestPath).HasMaxLength(500);
            b.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            b.HasIndex(x => new { x.EventType, x.CreatedAtUtc });
            b.HasIndex(x => x.IpAddress);
        });

        // Phase 5: FraudAlert
        builder.Entity<FraudAlert>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.AlertType).HasMaxLength(50).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(450);
            b.Property(x => x.OrderIds).HasMaxLength(500);
            b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            b.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.ResolvedBy).HasMaxLength(450);
            b.Property(x => x.ResolutionNotes).HasMaxLength(2000);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        });

        // Phase 5: ModerationFlag
        builder.Entity<ModerationFlag>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ContentType).HasMaxLength(30).IsRequired();
            b.Property(x => x.FlagType).HasMaxLength(50).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            b.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.ResolvedBy).HasMaxLength(450);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.ContentType, x.ContentId });
        });

        // Business Advisor: BusinessGoal
        builder.Entity<BusinessGoal>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Title).HasMaxLength(500).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.TargetMetric).HasMaxLength(100);
            b.Property(x => x.TargetValue).HasColumnType("decimal(18,2)");
            b.Property(x => x.CurrentProgress).HasColumnType("decimal(18,2)");
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => new { x.StoreUserId, x.Status });
        });

        // Business Advisor: BusinessRecommendation
        builder.Entity<BusinessRecommendation>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.Title).HasMaxLength(500).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
            b.Property(x => x.Priority).HasConversion<string>().HasMaxLength(30);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            b.Property(x => x.VerificationCriteria).HasMaxLength(2000);
            b.Property(x => x.VerificationResult).HasMaxLength(2000);
            b.HasOne(x => x.Goal).WithMany().HasForeignKey(x => x.GoalId).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => new { x.StoreUserId, x.Status });
            b.HasIndex(x => x.CreatedAtUtc);
        });

        // Business Advisor: BusinessAdvisorReport
        builder.Entity<BusinessAdvisorReport>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.ReportType).HasMaxLength(30).IsRequired();
            b.Property(x => x.ReportContent).IsRequired();
            b.Property(x => x.ReportDataJson);
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => new { x.StoreUserId, x.ReportType, x.CreatedAtUtc });
        });

        // Business Advisor: BusinessAdvisorMemory
        builder.Entity<BusinessAdvisorMemory>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.MemoryType).HasMaxLength(50).IsRequired();
            b.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => new { x.StoreUserId, x.MemoryType });
            b.HasIndex(x => x.ExpiresAtUtc);
        });

        // Store Analytics Configurations
        builder.Entity<StoreAnalyticsEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            b.HasIndex(x => x.StoreUserId);
            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => new { x.StoreUserId, x.CreatedAtUtc });
        });

        builder.Entity<StoreAnalyticsDaily>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TotalSales).HasColumnType("decimal(18,2)");
            b.Property(x => x.AverageOrderValue).HasColumnType("decimal(18,2)");
            b.HasIndex(x => new { x.StoreUserId, x.Date }).IsUnique();
            b.HasIndex(x => x.Date);
        });

        builder.Entity<StoreAnalyticsMonthly>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TotalSales).HasColumnType("decimal(18,2)");
            b.Property(x => x.AverageOrderValue).HasColumnType("decimal(18,2)");
            b.HasIndex(x => new { x.StoreUserId, x.Year, x.Month }).IsUnique();
        });

        builder.Entity<StoreAnalyticsYearly>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.StoreUserId).HasMaxLength(450).IsRequired();
            b.Property(x => x.TotalSales).HasColumnType("decimal(18,2)");
            b.Property(x => x.AverageOrderValue).HasColumnType("decimal(18,2)");
            b.HasIndex(x => new { x.StoreUserId, x.Year }).IsUnique();
        });
    }

    private bool _isSavingAnalytics;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isSavingAnalytics)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        var orderChanges = ChangeTracker.Entries<Order>()
            .Select(e => new { e.Entity, e.State, OriginalStatus = e.OriginalValues.GetValue<OrderStatus>(nameof(Order.Status)) })
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        var eventsToAdd = new List<StoreAnalyticsEvent>();
        var orderIdsToRemoveEvents = new List<int>();

        foreach (var change in orderChanges)
        {
            var order = change.Entity;
            if (change.State == EntityState.Added && order.Status != OrderStatus.Cancelled)
            {
                var items = order.Items;
                if (items == null || items.Count == 0)
                {
                    items = await OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync(cancellationToken);
                }

                foreach (var item in items)
                {
                    eventsToAdd.Add(new StoreAnalyticsEvent
                    {
                        StoreUserId = order.StoreUserId,
                        UserId = order.UserId,
                        OrderId = order.Id,
                        ProductId = item.ProductId ?? 0,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalAmount = item.Quantity * item.UnitPrice,
                        CreatedAtUtc = order.CreatedAtUtc
                    });
                }
            }
            else if (change.State == EntityState.Modified && order.Status == OrderStatus.Cancelled && change.OriginalStatus != OrderStatus.Cancelled)
            {
                orderIdsToRemoveEvents.Add(order.Id);
            }
        }

        if (eventsToAdd.Count > 0 || orderIdsToRemoveEvents.Count > 0)
        {
            _isSavingAnalytics = true;
            try
            {
                if (orderIdsToRemoveEvents.Count > 0)
                {
                    await StoreAnalyticsEvents
                        .Where(e => orderIdsToRemoveEvents.Contains(e.OrderId))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                if (eventsToAdd.Count > 0)
                {
                    await StoreAnalyticsEvents.AddRangeAsync(eventsToAdd, cancellationToken);
                }

                await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _isSavingAnalytics = false;
            }
        }

        return result;
    }

    public override int SaveChanges()
    {
        if (_isSavingAnalytics)
        {
            return base.SaveChanges();
        }

        var orderChanges = ChangeTracker.Entries<Order>()
            .Select(e => new { e.Entity, e.State, OriginalStatus = e.OriginalValues.GetValue<OrderStatus>(nameof(Order.Status)) })
            .ToList();

        var result = base.SaveChanges();

        var eventsToAdd = new List<StoreAnalyticsEvent>();
        var orderIdsToRemoveEvents = new List<int>();

        foreach (var change in orderChanges)
        {
            var order = change.Entity;
            if (change.State == EntityState.Added && order.Status != OrderStatus.Cancelled)
            {
                var items = order.Items;
                if (items == null || items.Count == 0)
                {
                    items = OrderItems.Where(oi => oi.OrderId == order.Id).ToList();
                }

                foreach (var item in items)
                {
                    eventsToAdd.Add(new StoreAnalyticsEvent
                    {
                        StoreUserId = order.StoreUserId,
                        UserId = order.UserId,
                        OrderId = order.Id,
                        ProductId = item.ProductId ?? 0,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalAmount = item.Quantity * item.UnitPrice,
                        CreatedAtUtc = order.CreatedAtUtc
                    });
                }
            }
            else if (change.State == EntityState.Modified && order.Status == OrderStatus.Cancelled && change.OriginalStatus != OrderStatus.Cancelled)
            {
                orderIdsToRemoveEvents.Add(order.Id);
            }
        }

        if (eventsToAdd.Count > 0 || orderIdsToRemoveEvents.Count > 0)
        {
            _isSavingAnalytics = true;
            try
            {
                if (orderIdsToRemoveEvents.Count > 0)
                {
                    var ids = orderIdsToRemoveEvents;
                    var eventsToDelete = StoreAnalyticsEvents.Where(e => ids.Contains(e.OrderId)).ToList();
                    StoreAnalyticsEvents.RemoveRange(eventsToDelete);
                }

                if (eventsToAdd.Count > 0)
                {
                    StoreAnalyticsEvents.AddRange(eventsToAdd);
                }

                base.SaveChanges();
            }
            finally
            {
                _isSavingAnalytics = false;
            }
        }

        return result;
    }

    private static List<string> SafeDeserializeAdditionalImageUrls(string? v)
    {
        if (string.IsNullOrWhiteSpace(v) || v.Length < 2)
            return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
