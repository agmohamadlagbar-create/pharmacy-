namespace OnlineStore.Api.Contracts;

public class SubmitFeedbackRequest
{
    public int ConversationId { get; set; }
    public int MessageId { get; set; }
    public int? Rating { get; set; }
    public string? FeedbackType { get; set; }
    public string? Comment { get; set; }
}

public class FeedbackSummaryDto
{
    public int TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<string, int> FeedbackTypeBreakdown { get; set; } = new();
    public Dictionary<string, double> AgentSatisfactionScores { get; set; } = new();
    public List<string> TopComplaints { get; set; } = new();
}
