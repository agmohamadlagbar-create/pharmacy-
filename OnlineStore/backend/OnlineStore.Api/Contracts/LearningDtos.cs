namespace OnlineStore.Api.Contracts;

public class ReviewSuggestionRequest
{
    public bool Approved { get; set; }
    public string? Notes { get; set; }
}

public class UpdateKnowledgeGapRequest
{
    public string Status { get; set; } = "detected";
    public string? Resolution { get; set; }
}

public class WeeklyReportDto
{
    public string Period { get; set; } = "";
    public double AverageSatisfaction { get; set; }
    public int TotalConversations { get; set; }
    public double SuccessRate { get; set; }
    public List<string> TopQuestions { get; set; } = new();
    public List<string> KnowledgeGaps { get; set; } = new();
    public Dictionary<string, double> AgentRanking { get; set; } = new();
    public int PendingSuggestions { get; set; }
}
