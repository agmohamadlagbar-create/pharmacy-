namespace OnlineStore.Domain.Entities;

public class AiConversationAnalysis
{
    public int Id { get; set; }
    public DateTime AnalysisDate { get; set; }

    public int TotalConversations { get; set; }
    public int SuccessfulConversations { get; set; }
    public int FailedConversations { get; set; }
    public int AbandonedConversations { get; set; }
    public double AverageSatisfactionRating { get; set; }
    public int TotalFeedbackCount { get; set; }
    public int NegativeFeedbackCount { get; set; }

    public string? AgentPerformanceJson { get; set; }
    public string? TopIssuesJson { get; set; }
    public string? CommonQuestionsJson { get; set; }
    public string? ToolPerformanceJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
