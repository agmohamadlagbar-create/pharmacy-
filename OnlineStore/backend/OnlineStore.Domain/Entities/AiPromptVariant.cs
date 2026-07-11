namespace OnlineStore.Domain.Entities;

public class AiPromptVariant
{
    public int Id { get; set; }
    public string AgentName { get; set; } = default!;
    public string VariantName { get; set; } = default!;
    public string SystemPrompt { get; set; } = default!;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }

    /// <summary>Groups variants into named experiments (e.g. "shopping_tone_test_v1").</summary>
    public string? ExperimentName { get; set; }

    /// <summary>0.0–1.0 traffic allocation. Default variant gets 0.8, test variant gets 0.2.</summary>
    public double TrafficPercentage { get; set; }

    /// <summary>Conversations where a positive outcome occurred (purchase, issue resolved, etc.).</summary>
    public int SuccessfulOutcomes { get; set; }

    /// <summary>All conversations that reached a conclusive end.</summary>
    public int TotalOutcomes { get; set; }

    public int TotalConversations { get; set; }
    public double AverageSatisfaction { get; set; }
    public double ConversionRate { get; set; }

    /// <summary>null (running) | "winner" | "loser" | "inconclusive"</summary>
    public string? WinnerDecision { get; set; }

    public DateTime? ExperimentStartedAtUtc { get; set; }
    public DateTime? ExperimentEndedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
