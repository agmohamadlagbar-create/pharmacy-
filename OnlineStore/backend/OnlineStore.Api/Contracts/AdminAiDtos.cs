namespace OnlineStore.Api.Contracts;

public record FraudAlertDto(
    int Id,
    string AlertType,
    string? UserId,
    string? UserEmail,
    string? OrderIds,
    string Description,
    string Severity,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc
);

public record FraudAlertUpdateRequest(string Status, string? ResolutionNotes);

public record ModerationFlagDto(
    int Id,
    string ContentType,
    int? ContentId,
    string FlagType,
    string Description,
    string Severity,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc
);

public record ModerationFlagUpdateRequest(string Status);

public record SecurityEventDto(
    int Id,
    string EventType,
    string? UserId,
    string? IpAddress,
    string? RequestPath,
    string Severity,
    DateTime CreatedAtUtc
);

public record PlatformOverviewDto(
    decimal TotalGmv,
    int TotalOrders,
    int TotalUsers,
    int TotalStores,
    int ActiveUsersLast30Days,
    int NewUsersThisMonth
);

public record StoreRankingDto(
    string StoreUserId,
    string ShopName,
    string? Location,
    decimal MetricValue
);

public record CategoryTrendDto(
    string CategoryName,
    decimal Revenue,
    int OrderCount,
    double GrowthRate
);

public record RegionalAnalyticsDto(
    string Region,
    int StoreCount,
    int CustomerCount,
    decimal Revenue
);

public record PendingStoreDto(
    string UserId,
    string ShopName,
    string? Location,
    string? OwnerName,
    string? Phone,
    DateTime CreatedAtUtc
);

public record StoreRiskScoreDto(
    string UserId,
    string ShopName,
    int Score,
    string Rating,
    List<string> RiskFactors
);

public record ComplianceCheckDto(
    bool EmailConfirmed,
    bool PhoneProvided,
    bool HasDescription,
    bool HasLocation,
    bool HasProducts,
    bool RapidUploadDetected,
    List<string> Warnings
);

public record FlaggedContentDto(
    int ContentId,
    string ContentType,
    string? Description,
    string FlagType,
    string Severity,
    DateTime CreatedAtUtc
);

public record ReviewModerationDto(
    int ReviewId,
    int ProductId,
    string? UserId,
    string? UserName,
    string? Comment,
    int Rating,
    DateTime CreatedAtUtc,
    List<string> Flags
);

public record PricingAnomalyDto(
    int ProductId,
    string ProductName,
    decimal Price,
    decimal CategoryAverage,
    double Ratio,
    string Severity
);

public record EscalatedIssueDto(
    int ConversationId,
    string UserId,
    string? UserEmail,
    string Summary,
    DateTime CreatedAtUtc
);

public record CrossStoreDisputeDto(
    int OrderId,
    string UserId,
    string? OrderGroupId,
    List<string> StoreIds,
    string Description,
    DateTime CreatedAtUtc
);

public record UserHistoryDto(
    string UserId,
    string? Email,
    string? FullName,
    DateTime CreatedAtUtc,
    int OrderCount,
    double CancellationRate,
    int FeedbackCount,
    int AiConversationCount,
    bool IsLockedOut
);
